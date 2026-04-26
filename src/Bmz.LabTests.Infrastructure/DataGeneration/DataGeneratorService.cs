using Bmz.LabTests.Application.Abstractions.DataGeneration;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Bmz.LabTests.Infrastructure.DataGeneration;

public sealed class DataGeneratorService(ApplicationDbContext dbContext) : IDataGeneratorService
{
    private static readonly Random Random = new();

    public async Task<Result<int>> GenerateTestResultsAsync(int count, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Fetch necessary data for generation
            var assistants = await dbContext.Users
                .Where(u => u.Role.Name == Roles.Assistant && u.LaboratoryId != null)
                .Select(u => new { u.Id, u.LaboratoryId })
                .ToListAsync(cancellationToken);

            if (assistants.Count == 0)
                return Result.Failure<int>("Не найдено лаборантов для генерации данных.");

            var wireCodesWithLimits = await dbContext.WireCodes
                .Include(wc => wc.Limits)
                .ThenInclude(l => l.Parameter)
                .ToListAsync(cancellationToken);

            if (wireCodesWithLimits.Count == 0)
                return Result.Failure<int>("Не найдено кодов проволоки с лимитами.");

            var customers = await dbContext.Customers
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            // 2. Generation process
            var batchSize = 500;
            var totalCreated = 0;
            var now = DateTime.UtcNow;
            var startDate = now.AddMonths(-18);

            for (int i = 0; i < count; i += batchSize)
            {
                var currentBatchSize = Math.Min(batchSize, count - i);
                var testResultsBatch = new List<TestResult>();
                var batchMetadata = new List<(TestResult Test, bool IsDefect, string? Reason, DateTime UpdatedAt)>();

                for (int j = 0; j < currentBatchSize; j++)
                {
                    var assistant = assistants[Random.Next(assistants.Count)];
                    var wireCode = wireCodesWithLimits[Random.Next(wireCodesWithLimits.Count)];
                    var customerId = customers.Count > 0 ? (int?)customers[Random.Next(customers.Count)] : null;

                    var date = GenerateRealisticDate(startDate, now);
                    var updatedAt = date.AddMinutes(Random.Next(15, 60));
                    var batchNumber = GenerateBatchNumber(wireCode.Code, date);

                    var testResult = new TestResult(
                        date,
                        updatedAt,
                        assistant.Id,
                        wireCode.Id,
                        assistant.LaboratoryId!.Value,
                        batchNumber,
                        customerId,
                        TestResultStatus.Completed);

                    var hasDefects = false;
                    var defectReasons = new List<string>();

                    // Generate values for each limit
                    foreach (var limit in wireCode.Limits)
                    {
                        var (value, isDefect) = GenerateRealisticValue(limit);
                        testResult.Values.Add(new TestValue(limit.ParameterId, value));
                        
                        if (isDefect)
                        {
                            hasDefects = true;
                            defectReasons.Add($"{limit.Parameter.Name}: {value} (норма {limit.MinValue ?? 0}-{limit.MaxValue ?? 0})");
                        }
                    }

                    testResultsBatch.Add(testResult);
                    batchMetadata.Add((testResult, hasDefects, hasDefects ? string.Join("; ", defectReasons) : null, updatedAt));
                }

                await dbContext.TestResults.AddRangeAsync(testResultsBatch, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                // Create FinalProduct/Reject records now that we have TestResult IDs
                foreach (var meta in batchMetadata)
                {
                    if (meta.IsDefect)
                    {
                        dbContext.Rejects.Add(new Reject(meta.Test.Id, meta.Reason!, meta.UpdatedAt));
                    }
                    else
                    {
                        dbContext.FinalProducts.Add(new FinalProduct(meta.Test.Id, meta.UpdatedAt));
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                totalCreated += currentBatchSize;
                
                // Clear tracker to avoid memory pressure for huge generations
                dbContext.ChangeTracker.Clear();
            }

            return Result.Success(totalCreated);
        }
        catch (Exception ex)
        {
            return Result.Failure<int>($"Ошибка при генерации данных: {ex.Message}");
        }
    }

    private static DateTime GenerateRealisticDate(DateTime start, DateTime end)
    {
        var range = end - start;
        var randomTimeSpan = new TimeSpan((long)(Random.NextDouble() * range.Ticks));
        var date = start + randomTimeSpan;

        // Simulate shifts: 80% day (08-20), 20% night
        var hour = Random.NextDouble() < 0.8 ? Random.Next(8, 20) : (Random.Next(0, 8) + (Random.Next(0, 2) * 20)) % 24;
        
        // Simulate weekend dip: 70% less records on weekends
        if ((date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) && Random.NextDouble() < 0.7)
        {
            // Move to a weekday
            date = date.AddDays(Random.Next(1, 6));
        }

        return new DateTime(date.Year, date.Month, date.Day, hour, Random.Next(0, 60), Random.Next(0, 60), DateTimeKind.Utc);
    }

    private static string GenerateBatchNumber(string wireCode, DateTime date)
    {
        var prefixes = new[] { "LOT", "П", "B", "A", "Z" };
        var prefix = prefixes[Random.Next(prefixes.Length)];
        return $"{prefix}-{date:yyyy}-{Random.Next(1000, 9999)}";
    }

    private static (string value, bool isDefect) GenerateRealisticValue(WireCodeLimit limit)
    {
        if (limit.Parameter.DataType != ParameterDataType.Number)
            return ("OK", false);

        var min = (double?)limit.MinValue ?? 0.0;
        var max = (double?)limit.MaxValue ?? 100.0;
        
        // If both are 0/100 (default), maybe the range is not set, let's use some reasonable defaults
        if (limit.MinValue == null && limit.MaxValue == null)
        {
            min = 10.0;
            max = 50.0;
        }
        else if (limit.MinValue == null)
        {
            min = max * 0.8;
        }
        else if (limit.MaxValue == null)
        {
            max = min * 1.2;
        }

        var mean = (min + max) / 2.0;
        var stdDev = (max - min) / 6.0; // 99.7% of values within [min, max] if normal
        if (stdDev <= 0) stdDev = mean * 0.05; // fallback for same min/max

        // Occasionally (2%) generate an outlier (defect)
        if (Random.NextDouble() < 0.02)
        {
            var isLow = Random.Next(0, 2) == 0;
            var outlier = isLow ? min - (max - min) * 0.1 : max + (max - min) * 0.1;
            return (outlier.ToString("F3", CultureInfo.InvariantCulture), true);
        }

        var value = NextGaussian(mean, stdDev);
        return (value.ToString("F3", CultureInfo.InvariantCulture), false);
    }

    private static double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0 - Random.NextDouble();
        double u2 = 1.0 - Random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * randStdNormal;
    }
}
