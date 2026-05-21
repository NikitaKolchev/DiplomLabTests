using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Reporting;

/// <summary>
/// Сервис для расчета аналитических показателей и формирования статистики.
/// Выполняет сложные агрегатные запросы к базе данных для оценки качества продукции и эффективности работы лабораторий.
/// </summary>
public sealed class StatisticsService(ApplicationDbContext dbContext) : IStatisticsService
{
    private sealed record ViolationCandidate(
        string Value,
        decimal? MinValue,
        decimal? MaxValue,
        int ParameterId,
        string ParameterName,
        string? Unit);

    /// <summary>
    /// Собирает полную статистику за период: общие KPI, срезы по подразделениям, маркам проволоки, 
    /// причины брака и динамику испытаний.
    /// </summary>
    public async Task<Result<StatisticsResponseDto>> GetAsync(
        DateTime fromUtc,
        DateTime toUtc,
        StatisticsTrendGroupBy groupBy,
        int? laboratoryId,
        CancellationToken cancellationToken)
    {
        try
        { 
            var baseQuery = dbContext.TestResults
                .AsNoTracking()
                .Where(x => x.Date >= fromUtc && x.Date < toUtc);

            if (laboratoryId.HasValue)
            {
                baseQuery = baseQuery.Where(x => x.LaboratoryId == laboratoryId.Value);
            }

            var overviewStats = await baseQuery
                .Select(x => new
                {
                    IsCompleted = x.Status == TestResultStatus.Completed,
                    IsRejected = dbContext.Rejects.Any(r => r.TestResultId == x.Id),
                    CycleMinutes = x.Status == TestResultStatus.Completed
                        ? (double?)EF.Functions.DateDiffMinute(x.Date, x.UpdatedAtUtc)
                        : null
                })
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Completed = g.Count(x => x.IsCompleted),
                    Rejected = g.Count(x => x.IsRejected),
                    AvgCycleMinutes = g.Average(x => x.CycleMinutes)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalTests = overviewStats?.Total ?? 0;
            var completedTests = overviewStats?.Completed ?? 0;
            var inProgressTests = totalTests - completedTests;
            var rejectedTests = overviewStats?.Rejected ?? 0;
            var avgCycleMinutes = overviewStats?.AvgCycleMinutes ?? 0d;

            var rejectRatePercent = completedTests == 0 ? 0m : Math.Round((decimal)rejectedTests * 100m / completedTests, 2);
            var acceptanceRatePercent = completedTests == 0 ? 0m : Math.Round((decimal)(completedTests - rejectedTests) * 100m / completedTests, 2);
            var avgCycleHours = Math.Round((decimal)(avgCycleMinutes / 60d), 2);

            var completedQuery = baseQuery.Where(x => x.Status == TestResultStatus.Completed);

            var laboratoryRows = await (
                from testResult in completedQuery
                join laboratory in dbContext.Laboratories.AsNoTracking() on testResult.LaboratoryId equals laboratory.Id
                join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
                from reject in rejectGroup.DefaultIfEmpty()
                group new { testResult, reject } by new { laboratory.Id, laboratory.Name }
                into g
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    CompletedCount = g.Count(),
                    RejectedCount = g.Count(x => x.reject != null),
                    AvgCycleMinutes = g.Average(x => (double?)EF.Functions.DateDiffMinute(x.testResult.Date, x.testResult.UpdatedAtUtc)) ?? 0d
                }
            ).ToListAsync(cancellationToken);

            var laboratorySlice = laboratoryRows
                .Select(x => new StatisticsLaboratorySliceDto(
                    x.Id,
                    x.Name,
                    x.CompletedCount,
                    x.RejectedCount,
                    x.CompletedCount == 0 ? 0m : Math.Round((decimal)x.RejectedCount * 100m / x.CompletedCount, 2),
                    Math.Round((decimal)(x.AvgCycleMinutes / 60d), 2)))
                .OrderByDescending(x => x.RejectRatePercent)
                .ThenByDescending(x => x.CompletedTests)
                .ToArray();

            var wireCodeRows = await (
                from testResult in completedQuery
                join wireCode in dbContext.WireCodes.AsNoTracking() on testResult.WireCodeId equals wireCode.Id
                join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
                from reject in rejectGroup.DefaultIfEmpty()
                group new { reject } by new { wireCode.Id, wireCode.Code }
                into g
                select new
                {
                    g.Key.Id,
                    g.Key.Code,
                    CompletedCount = g.Count(),
                    RejectedCount = g.Count(x => x.reject != null)
                }
            ).ToListAsync(cancellationToken);

            var wireCodeSlice = wireCodeRows
                .Select(x => new StatisticsWireCodeSliceDto(
                    x.Id,
                    x.Code,
                    x.CompletedCount,
                    x.RejectedCount,
                    x.CompletedCount == 0 ? 0m : Math.Round((decimal)x.RejectedCount * 100m / x.CompletedCount, 2)))
                .OrderByDescending(x => x.RejectRatePercent)
                .ThenByDescending(x => x.CompletedTests)
                .ToArray();

            var rejectReasonRows = await (
                from testResult in completedQuery
                join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId
                group reject by reject.Reason into g
                select new
                {
                    Reason = g.Key,
                    Count = g.Count()
                }
            )
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            var rejectReasons = rejectReasonRows
                .Select(x => new StatisticsRejectReasonDto(
                    x.Reason,
                    x.Count,
                    rejectedTests == 0 ? 0m : Math.Round((decimal)x.Count * 100m / rejectedTests, 2)))
                .ToArray();

            var trendRows = await (
                from testResult in baseQuery
                join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
                from reject in rejectGroup.DefaultIfEmpty()
                select new
                {
                    testResult.Date,
                    testResult.Status,
                    IsRejected = reject != null
                }
            ).ToListAsync(cancellationToken);

            var trends = trendRows
                .GroupBy(x => GetPeriodStartUtc(x.Date, groupBy))
                .OrderBy(x => x.Key)
                .Select(g => new StatisticsTrendPointDto(
                    g.Key,
                    g.Count(),
                    g.Count(x => x.Status == TestResultStatus.Completed),
                    g.Count(x => x.IsRejected)))
                .ToArray();

            var assistantRows = await (
                from testResult in completedQuery
                join user in dbContext.Users.AsNoTracking() on testResult.AssistantId equals user.Id
                join laboratory in dbContext.Laboratories.AsNoTracking() on testResult.LaboratoryId equals laboratory.Id
                group testResult by new { user.Id, user.FullName, LaboratoryName = laboratory.Name }
                into g
                select new
                {
                    g.Key.Id,
                    g.Key.FullName,
                    g.Key.LaboratoryName,
                    CompletedCount = g.Count(),
                    AvgCycleMinutes = g.Average(x => (double?)EF.Functions.DateDiffMinute(x.Date, x.UpdatedAtUtc)) ?? 0d
                }
            )
                .OrderByDescending(x => x.CompletedCount)
                .ThenBy(x => x.FullName)
                .Take(20)
                .ToListAsync(cancellationToken);

            var assistantCycles = assistantRows
                .Select(x => new StatisticsCycleByAssistantDto(
                    x.Id,
                    x.FullName,
                    x.LaboratoryName,
                    x.CompletedCount,
                    Math.Round((decimal)(x.AvgCycleMinutes / 60d), 2)))
                .ToArray();

            var parameterViolationRows = await (
                from testResult in completedQuery
                join value in dbContext.TestValues.AsNoTracking() on testResult.Id equals value.TestResultId
                join limit in dbContext.Limits.AsNoTracking()
                    on new { testResult.WireCodeId, value.ParameterId } equals new { limit.WireCodeId, limit.ParameterId }
                join parameter in dbContext.Parameters.AsNoTracking() on value.ParameterId equals parameter.Id
                select new ViolationCandidate(
                    value.Value,
                    limit.MinValue,
                    limit.MaxValue,
                    parameter.Id,
                    parameter.Name,
                    parameter.Unit)
            ).ToListAsync(cancellationToken);

            var violations = parameterViolationRows
                .Where(IsOutOfSpec)
                .GroupBy(x => new { x.ParameterId, x.ParameterName, x.Unit })
                .Select(g => new StatisticsParameterViolationDto(
                    g.Key.ParameterId,
                    g.Key.ParameterName,
                    g.Key.Unit,
                    g.Count(),
                    completedTests == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / completedTests, 2)))
                .OrderByDescending(x => x.OutOfSpecCount)
                .ToArray();

            return Result.Success(new StatisticsResponseDto(
                new StatisticsOverviewDto(
                    totalTests,
                    completedTests,
                    inProgressTests,
                    rejectedTests,
                    rejectRatePercent,
                    acceptanceRatePercent,
                    avgCycleHours),
                laboratorySlice,
                wireCodeSlice,
                violations,
                trends,
                rejectReasons,
                assistantCycles));
        }
        catch (Exception ex)
        {
            return Result.Failure<StatisticsResponseDto>($"Ошибка при получении статистики: {ex.Message}");
        }
    }

    private static DateTime GetPeriodStartUtc(DateTime dateUtc, StatisticsTrendGroupBy groupBy)
    {
        var day = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        return groupBy switch
        {
            StatisticsTrendGroupBy.Day => day,
            StatisticsTrendGroupBy.Week => day.AddDays(-((7 + (int)day.DayOfWeek - (int)DayOfWeek.Monday) % 7)),
            StatisticsTrendGroupBy.Month => new DateTime(day.Year, day.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => day
        };
    }

    private static bool IsOutOfSpec(ViolationCandidate row)
    {
        if (row.MinValue is null && row.MaxValue is null)
            return false;

        if (!decimal.TryParse(
                row.Value,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var numericValue))
        {
            return false;
        }

        var min = row.MinValue;
        var max = row.MaxValue;
        if (min.HasValue && numericValue < min.Value) return true;
        if (max.HasValue && numericValue > max.Value) return true;
        return false;
    }
}
