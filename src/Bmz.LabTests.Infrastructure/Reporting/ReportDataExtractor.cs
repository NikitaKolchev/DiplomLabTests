using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Application.Abstractions.Testing;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Reporting;

public sealed record StatisticsPdfData(
    int TotalTests,
    int CompletedTests,
    int InProgressTests,
    int AcceptedTests,
    int RejectedTests,
    decimal RejectRatePercent,
    decimal AcceptanceRatePercent,
    decimal AvgCycleHours,
    IReadOnlyList<TrendPoint> Trends,
    IReadOnlyList<LabStatItem> LabStats,
    IReadOnlyList<WireStatItem> WireStats,
    string LabName);

public sealed record TrendPoint(DateTime Period, int Total);

public sealed record LabStatItem(string Name, int Completed, int Rejected);

public sealed record WireStatItem(string Code, int Completed, int Rejected);

/// <summary>
/// Промежуточный сервис для извлечения и подготовки данных для PDF-отчетов.
/// Оптимизирует выборку данных из БД перед передачей в генератор документов.
/// </summary>
public sealed class ReportDataExtractor
{
    private readonly Persistence.ApplicationDbContext _dbContext;

    public ReportDataExtractor(Persistence.ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Собирает упрощенный набор данных для базовой версии PDF-отчета.
    /// </summary>
    public async Task<StatisticsPdfData> ExtractStatisticsDataAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken)
    {
        var baseQuery = _dbContext.TestResults
            .AsNoTracking()
            .Where(x => x.Date >= fromUtc && x.Date < toUtc);

        if (laboratoryId.HasValue)
            baseQuery = baseQuery.Where(x => x.LaboratoryId == laboratoryId.Value);

        var totalTests = await baseQuery.CountAsync(cancellationToken);
        var completedQuery = baseQuery.Where(x => x.Status == TestResultStatus.Completed);
        var completedTests = await completedQuery.CountAsync(cancellationToken);
        var inProgressTests = totalTests - completedTests;

        var rejectedTests = await (
            from testResult in completedQuery
            join reject in _dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId
            select reject.Id
        ).CountAsync(cancellationToken);

        var acceptedTests = completedTests - rejectedTests;
        var rejectRatePercent = completedTests == 0 ? 0m : Math.Round((decimal)rejectedTests * 100m / completedTests, 1);
        var acceptanceRatePercent = completedTests == 0 ? 0m : Math.Round((decimal)acceptedTests * 100m / completedTests, 1);

        var avgCycleMinutes = await completedQuery
            .Select(x => (double?)EF.Functions.DateDiffMinute(x.Date, x.UpdatedAtUtc))
            .AverageAsync(cancellationToken) ?? 0d;
        var avgCycleHours = Math.Round((decimal)(avgCycleMinutes / 60d), 1);

        var trends = await completedQuery
            .Select(x => new { x.Date, x.Status })
            .ToListAsync(cancellationToken);

        var groupedTrends = groupBy switch
        {
            StatisticsTrendGroupBy.Day => trends.GroupBy(x => x.Date.Date).OrderBy(g => g.Key).Take(30).ToList(),
            StatisticsTrendGroupBy.Week => trends.GroupBy(x => GetWeekStart(x.Date)).OrderBy(g => g.Key).Take(12).ToList(),
            StatisticsTrendGroupBy.Month => trends.GroupBy(x => new DateTime(x.Date.Year, x.Date.Month, 1)).OrderBy(g => g.Key).Take(12).ToList(),
            _ => trends.GroupBy(x => x.Date.Date).OrderBy(g => g.Key).Take(30).ToList()
        };

        var trendPoints = groupedTrends
            .Select(g => new TrendPoint(g.Key, g.Count()))
            .ToList();

        var labStats = await (
            from testResult in completedQuery
            join lab in _dbContext.Laboratories.AsNoTracking() on testResult.LaboratoryId equals lab.Id
            join reject in _dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
            from reject in rejectGroup.DefaultIfEmpty()
            group new { testResult, reject } by new { lab.Id, lab.Name }
            into g
            select new LabStatItem(g.Key.Name, g.Count(), g.Count(x => x.reject != null))
        ).ToListAsync(cancellationToken);

        var wireStats = await (
            from testResult in completedQuery
            join wire in _dbContext.WireCodes.AsNoTracking() on testResult.WireCodeId equals wire.Id
            join reject in _dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
            from reject in rejectGroup.DefaultIfEmpty()
            group new { testResult, reject } by new { wire.Id, wire.Code }
            into g
            select new WireStatItem(g.Key.Code, g.Count(), g.Count(x => x.reject != null))
        ).ToListAsync(cancellationToken);

        var labName = laboratoryId.HasValue
            ? await _dbContext.Laboratories.Where(x => x.Id == laboratoryId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : "Все лаборатории";

        return new StatisticsPdfData(
            totalTests,
            completedTests,
            inProgressTests,
            acceptedTests,
            rejectedTests,
            rejectRatePercent,
            acceptanceRatePercent,
            avgCycleHours,
            trendPoints,
            labStats,
            wireStats,
            labName ?? "Все лаборатории");
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}