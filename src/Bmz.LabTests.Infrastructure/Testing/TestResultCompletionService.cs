using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Bmz.LabTests.Infrastructure.Testing;

public sealed class TestResultCompletionService(ApplicationDbContext dbContext) : ITestResultCompletionService
{
    public async Task<Result<CompletionResult>> CompleteAsync(int testResultId, byte[] rowVersion, CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var testResult = await dbContext.TestResults
                .Include(x => x.Values)
                .FirstOrDefaultAsync(x => x.Id == testResultId, cancellationToken);

            if (testResult is null)
            {
                return Result.Failure<CompletionResult>("Результат испытания не найден.");
            }

            if (testResult.Status == TestResultStatus.Completed)
            {
                return Result.Failure<CompletionResult>("Результат испытания уже завершен.");
            }

            dbContext.Entry(testResult).Property(x => x.RowVersion).OriginalValue = rowVersion;

            var limits = await dbContext.Limits
                .Include(x => x.Parameter)
                .Where(x => x.WireCodeId == testResult.WireCodeId)
                .ToListAsync(cancellationToken);

            var valueLookup = testResult.Values.ToDictionary(x => x.ParameterId, x => x.Value);
            var issues = Validate(limits, valueLookup);

            testResult.Complete();

            if (issues.Count == 0)
            {
                dbContext.FinalProducts.Add(new FinalProduct(testResultId));
            }
            else
            {
                var reason = string.Join("; ", issues);
                dbContext.Rejects.Add(new Reject(testResultId, reason));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success(new CompletionResult
            {
                IsAccepted = issues.Count == 0,
                RejectReason = issues.Count == 0 ? null : string.Join("; ", issues)
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<CompletionResult>("Данные были изменены другим пользователем. Пожалуйста, обновите страницу.");
        }
        catch (Exception ex)
        {
            return Result.Failure<CompletionResult>($"Ошибка при завершении испытания: {ex.Message}");
        }
    }

    private static List<string> Validate(IReadOnlyCollection<WireCodeLimit> limits, IReadOnlyDictionary<int, string> values)
    {
        var issues = new List<string>();
        var culture = CultureInfo.InvariantCulture;

        foreach (var limit in limits)
        {
            values.TryGetValue(limit.ParameterId, out var rawValue);
            var hasValue = !string.IsNullOrWhiteSpace(rawValue);

            if (limit.IsRequired && !hasValue)
            {
                issues.Add($"{limit.Parameter.Name}: значение обязательно.");
                continue;
            }

            if (!hasValue)
            {
                continue;
            }

            if (limit.Parameter.DataType != ParameterDataType.Number)
            {
                continue;
            }

            var normalized = rawValue!.Trim().Replace(',', '.');
            if (!decimal.TryParse(normalized, NumberStyles.Number, culture, out var numericValue))
            {
                issues.Add($"{limit.Parameter.Name}: '{rawValue}' не является корректным числом.");
                continue;
            }

            if (limit.MinValue.HasValue && numericValue < limit.MinValue.Value)
            {
                issues.Add($"{limit.Parameter.Name}: {numericValue} меньше {limit.MinValue.Value}.");
            }

            if (limit.MaxValue.HasValue && numericValue > limit.MaxValue.Value)
            {
                issues.Add($"{limit.Parameter.Name}: {numericValue} больше {limit.MaxValue.Value}.");
            }
        }

        return issues;
    }
}
