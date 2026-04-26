using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bmz.LabTests.Infrastructure.Reporting;

public sealed class ReportService(ApplicationDbContext dbContext) : IReportService
{
    private const string CompanyName = "ОАО «БМЗ» — Сталепроволочный цех";
    private const string ReportTitle = "Система регистрации лабораторных испытаний";

    public async Task<Result<ReportFile>> GenerateStatisticsPdfAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

        var baseQuery = dbContext.TestResults
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
            join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId
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
            .Select(g => new { Period = g.Key, Total = g.Count() })
            .ToList();

        var labStats = await (
            from testResult in completedQuery
            join lab in dbContext.Laboratories.AsNoTracking() on testResult.LaboratoryId equals lab.Id
            join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
            from reject in rejectGroup.DefaultIfEmpty()
            group new { testResult, reject } by new { lab.Id, lab.Name }
            into g
            select new { g.Key.Name, Completed = g.Count(), Rejected = g.Count(x => x.reject != null) }
        ).ToListAsync(cancellationToken);

        var wireStats = await (
            from testResult in completedQuery
            join wire in dbContext.WireCodes.AsNoTracking() on testResult.WireCodeId equals wire.Id
            join reject in dbContext.Rejects.AsNoTracking() on testResult.Id equals reject.TestResultId into rejectGroup
            from reject in rejectGroup.DefaultIfEmpty()
            group new { testResult, reject } by new { wire.Id, wire.Code }
            into g
            select new { g.Key.Code, Completed = g.Count(), Rejected = g.Count(x => x.reject != null) }
        ).ToListAsync(cancellationToken);

        var primaryColor = Color.FromHex("#2c5282");
        var headerBg = Color.FromHex("#2c5282");
        var headerText = Colors.White;
        var borderColor = Color.FromHex("#cbd5e0");
        var accentGreen = Colors.Green.Medium;
        var accentRed = Colors.Red.Medium;
        var accentBlue = Colors.Blue.Medium;

        var labName = laboratoryId.HasValue
            ? await dbContext.Laboratories.Where(x => x.Id == laboratoryId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : "Все лаборатории";

        var groupByName = groupBy switch
        {
            StatisticsTrendGroupBy.Day => "по дням",
            StatisticsTrendGroupBy.Week => "по неделям",
            StatisticsTrendGroupBy.Month => "по месяцам",
            _ => "по дням"
        };

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().PaddingBottom(5).AlignCenter().Text(CompanyName).FontSize(12).Bold().FontColor(primaryColor);
                    col.Item().AlignCenter().Text(ReportTitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(3).AlignCenter().Text("ОТЧЕТ ПО СТАТИСТИКЕ").FontSize(14).Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Border(1).BorderColor(borderColor).Padding(10).Column(summary =>
                    {
                        summary.Item().Text("ПЕРИОД И ФИЛЬТРЫ").FontSize(10).Bold().FontColor(primaryColor);
                        summary.Item().PaddingTop(5).Text($"Период: {fromUtc:dd.MM.yyyy} — {toUtc:dd.MM.yyyy}").FontSize(10);
                        summary.Item().Text($"Группировка: {groupByName}").FontSize(10);
                        summary.Item().Text($"Лаборатория: {labName}").FontSize(10);
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ВСЕГО ИСПЫТАНИЙ").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(totalTests.ToString()).FontSize(24).Bold().FontColor(primaryColor);
                        });
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАВЕРШЕНО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(completedTests.ToString()).FontSize(24).Bold().FontColor(accentGreen);
                        });
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("В РАБОТЕ").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(inProgressTests.ToString()).FontSize(24).Bold().FontColor(accentBlue);
                        });
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ПРИНЯТО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{acceptedTests} ({acceptanceRatePercent}%)").FontSize(20).Bold().FontColor(accentGreen);
                        });
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАБРАКОВАНО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{rejectedTests} ({rejectRatePercent}%)").FontSize(20).Bold().FontColor(accentRed);
                        });
                        row.RelativeItem().Border(1).BorderColor(borderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("СР. ВРЕМЯ ЦИКЛА").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{avgCycleHours} ч").FontSize(20).Bold();
                        });
                    });

                    col.Item().PaddingTop(10).Text("ДИНАМИКА ИСПЫТАНИЙ").FontSize(11).Bold().FontColor(primaryColor);
                    col.Item().Border(1).BorderColor(borderColor).Padding(10).Column(chartCol =>
                    {
                        if (trendPoints.Count > 0)
                        {
                            var maxVal = trendPoints.Max(x => x.Total);
                            foreach (var point in trendPoints)
                            {
                                var barWidth = maxVal > 0 ? (int)(point.Total * 30.0 / maxVal) : 0;
                                var label = groupBy == StatisticsTrendGroupBy.Month
                                    ? point.Period.ToString("MMM yy")
                                    : groupBy == StatisticsTrendGroupBy.Week
                                        ? point.Period.ToString("dd.MM")
                                        : point.Period.ToString("dd.MM");
                                chartCol.Item().Row(r =>
                                {
                                    r.AutoItem().Text(label).FontSize(8);
                                    r.AutoItem().Width(barWidth).Height(10).Background(primaryColor);
                                    r.AutoItem().PaddingLeft(3).Text(point.Total.ToString()).FontSize(8);
                                });
                            }
                        }
                        else
                        {
                            chartCol.Item().Text("Нет данных").FontColor(Colors.Grey.Medium);
                        }
                    });

                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(labCol =>
                        {
                            labCol.Item().Text("ПО ЛАБОРАТОРИЯМ").FontSize(11).Bold().FontColor(primaryColor);
                            labCol.Item().Border(1).BorderColor(borderColor).Padding(5).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(headerBg).Padding(4).Text("Лаборатория").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("Всего").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("Брак").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("%").FontColor(headerText).FontSize(8).Bold();
                                });
                                foreach (var lab in labStats.Take(8))
                                {
                                    var rate = lab.Completed == 0 ? 0 : Math.Round((decimal)lab.Rejected * 100 / lab.Completed, 1);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(lab.Name).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(lab.Completed.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(lab.Rejected.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text($"{rate}%").FontSize(8).FontColor(rate > 10 ? accentRed : Colors.Black);
                                }
                            });
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Column(wireCol =>
                        {
                            wireCol.Item().Text("ПО КОДАМ ПРОВОЛОКИ").FontSize(11).Bold().FontColor(primaryColor);
                            wireCol.Item().Border(1).BorderColor(borderColor).Padding(5).Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                table.Header(h =>
                                {
                                    h.Cell().Background(headerBg).Padding(4).Text("Код").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("Всего").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("Брак").FontColor(headerText).FontSize(8).Bold();
                                    h.Cell().Background(headerBg).Padding(4).Text("%").FontColor(headerText).FontSize(8).Bold();
                                });
                                foreach (var wire in wireStats.OrderByDescending(x => x.Completed).Take(8))
                                {
                                    var rate = wire.Completed == 0 ? 0 : Math.Round((decimal)wire.Rejected * 100 / wire.Completed, 1);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(wire.Code).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(wire.Completed.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(wire.Rejected.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text($"{rate}%").FontSize(8).FontColor(rate > 10 ? accentRed : Colors.Black);
                                }
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Column(c =>
                {
                    c.Item().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                    c.Item().Text("Статистический отчет — ОАО «БМЗ»").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });
        }).GeneratePdf();

            return Result.Success(new ReportFile
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"statistics-{fromUtc:yyyy-MM-dd}-{toUtc:yyyy-MM-dd}.pdf"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации отчета: {ex.Message}");
        }
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }

    public async Task<Result<ReportFile>> GenerateStatisticsPdfWithChartsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        StatisticsResponseDto statistics,
        CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

        var primaryColor = Color.FromHex("#2c5282");
        var headerBg = Color.FromHex("#2c5282");
        var headerText = Colors.White;
        var borderColor = Color.FromHex("#cbd5e0");
        var accentGreen = Colors.Green.Medium;
        var accentRed = Colors.Red.Medium;
        var accentBlue = Colors.Blue.Medium;
        var accentOrange = Colors.Orange.Medium;

        var groupByName = groupBy switch
        {
            StatisticsTrendGroupBy.Day => "по дням",
            StatisticsTrendGroupBy.Week => "по неделям",
            StatisticsTrendGroupBy.Month => "по месяцам",
            _ => "по дням"
        };

        var labName = laboratoryId.HasValue
            ? await dbContext.Laboratories.Where(x => x.Id == laboratoryId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
            : "Все лаборатории";

        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);

                page.Header().Column(col =>
                {
                    col.Item().PaddingBottom(3).AlignCenter().Text(CompanyName).FontSize(11).Bold().FontColor(primaryColor);
                    col.Item().AlignCenter().Text(ReportTitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(2).AlignCenter().Text("ОТЧЕТ ПО СТАТИСТИКЕ").FontSize(13).Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Border(1).BorderColor(borderColor).Padding(8).Column(filterCol =>
                    {
                        filterCol.Item().Text("ПЕРИОД И ФИЛЬТРЫ").FontSize(9).Bold().FontColor(primaryColor);
                        filterCol.Item().PaddingTop(3).Text($"Период: {fromUtc:dd.MM.yyyy} — {toUtc:dd.MM.yyyy}").FontSize(9);
                        filterCol.Item().Text($"Группировка: {groupByName}").FontSize(9);
                        filterCol.Item().Text($"Лаборатория: {labName}").FontSize(9);
                    });

                    if (statistics.Overview != null)
                    {
                        var o = statistics.Overview;
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("ВСЕГО").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text(o.TotalTests.ToString()).FontSize(22).Bold().FontColor(primaryColor);
                            });
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("ЗАВЕРШЕНО").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text(o.CompletedTests.ToString()).FontSize(22).Bold().FontColor(accentGreen);
                            });
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("В РАБОТЕ").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text(o.InProgressTests.ToString()).FontSize(22).Bold().FontColor(accentBlue);
                            });
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("ПРИНЯТО").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text($"{o.AcceptanceRatePercent}%").FontSize(22).Bold().FontColor(accentGreen);
                            });
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("ЗАБРАКОВАНО").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text($"{o.RejectRatePercent}%").FontSize(22).Bold().FontColor(accentRed);
                            });
                            row.RelativeItem().Border(1).BorderColor(borderColor).Padding(8).Column(c =>
                            {
                                c.Item().Text("СР.ЦИКЛ").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                                c.Item().PaddingTop(2).Text($"{o.AvgCycleHours} ч").FontSize(18).Bold();
                            });
                        });
                    }

                    if (statistics.Trends?.Count > 0)
                    {
                        col.Item().Text("ТРЕНД ИСПЫТАНИЙ").FontSize(10).Bold().FontColor(primaryColor);
                        col.Item().Border(1).BorderColor(borderColor).Padding(10).Column(chartCol =>
                        {
                            var maxVal = statistics.Trends.Max(x => x.TotalTests);
                            var bars = statistics.Trends.Take(20).ToList();
                            
                            chartCol.Item().PaddingBottom(5).Row(legendRow =>
                            {
                                legendRow.RelativeItem().Row(legend =>
                                {
                                    legend.AutoItem().Width(12).Height(8).Background(primaryColor);
                                    legend.AutoItem().Text(" Всего").FontSize(7);
                                    legend.AutoItem().Width(12).Height(8).Background(accentGreen);
                                    legend.AutoItem().Text(" Принято").FontSize(7);
                                    legend.AutoItem().Width(12).Height(8).Background(accentRed);
                                    legend.AutoItem().Text(" Брак").FontSize(7);
                                });
                            });
                            
                            foreach (var point in bars)
                            {
                                var totalW = maxVal > 0 ? (int)(point.TotalTests * 60.0 / maxVal) : 0;
                                var goodW = maxVal > 0 ? (int)((point.TotalTests - point.RejectedTests) * 60.0 / maxVal) : 0;
                                var badW = maxVal > 0 ? (int)(point.RejectedTests * 60.0 / maxVal) : 0;
                                var label = groupBy == StatisticsTrendGroupBy.Month
                                    ? point.PeriodStartUtc.ToString("MMM")
                                    : groupBy == StatisticsTrendGroupBy.Week
                                        ? point.PeriodStartUtc.ToString("dd/MM")
                                        : point.PeriodStartUtc.ToString("dd/MM");
                                        
                                chartCol.Item().Row(r =>
                                {
                                    r.AutoItem().PaddingRight(5).Text(label).FontSize(6);
                                    r.RelativeItem().Height(12).LineVertical(0.5f).LineColor(borderColor);
                                    r.RelativeItem().Height(12).Background(primaryColor).Width(totalW > 0 ? 1 : 0);
                                    if (goodW > 0)
                                        r.RelativeItem().Width(goodW).Height(12).Background(accentGreen);
                                    if (badW > 0)
                                        r.RelativeItem().Width(badW).Height(12).Background(accentRed).PaddingLeft(1);
                                    r.AutoItem().PaddingLeft(3).Text($"{point.TotalTests}").FontSize(7).Bold();
                                });
                            }
                        });
                    }

                    if (statistics.Overview != null)
                    {
                        var o = statistics.Overview;
                        col.Item().PaddingTop(10).Text("СООТНОШЕНИЕ").FontSize(10).Bold().FontColor(primaryColor);
                        col.Item().Border(1).BorderColor(borderColor).Padding(15).Row(pieRow =>
                        {
                            pieRow.RelativeItem().Column(pie1 =>
                            {
                                pie1.Item().Text("Принято vs Брак").FontSize(9).Bold().AlignCenter();
                                pie1.Item().PaddingTop(5).Row(r1 =>
                                {
                                    r1.RelativeItem().Background(accentGreen).Height(25).Text($"  {o.AcceptanceRatePercent}%  ").FontSize(14).Bold().FontColor(Colors.White).AlignCenter();
                                    r1.RelativeItem().Background(accentRed).Height(25).Text($"  {o.RejectRatePercent}%  ").FontSize(14).Bold().FontColor(Colors.White).AlignCenter();
                                });
                                pie1.Item().PaddingTop(3).Text($"{o.CompletedTests - o.RejectedTests} принято / {o.RejectedTests} брак").FontSize(8).AlignCenter();
                            });
                            
                            pieRow.RelativeItem().Column(pie2 =>
                            {
                                pie2.Item().Text("Завершено vs В работе").FontSize(9).Bold().AlignCenter();
                                pie2.Item().PaddingTop(5).Row(r2 =>
                                {
                                    r2.RelativeItem().Background(accentGreen).Height(25).Text($"  {o.CompletedTests}  ").FontSize(14).Bold().FontColor(Colors.White).AlignCenter();
                                    r2.RelativeItem().Background(accentBlue).Height(25).Text($"  {o.InProgressTests}  ").FontSize(14).Bold().FontColor(Colors.White).AlignCenter();
                                });
                                pie2.Item().PaddingTop(3).Text($"Завершено / В работе").FontSize(8).AlignCenter();
                            });
                        });
                    }
                });

                page.Footer().AlignCenter().Column(c =>
                {
                    c.Item().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(CompanyName).FontSize(11).Bold().FontColor(primaryColor);
                    col.Item().AlignCenter().Text("СТАТИСТИКА ПО ОБЪЕКТАМ").FontSize(12).Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);

                    if (statistics.Laboratories?.Count > 0)
                    {
                        col.Item().Text("ПО ЛАБОРАТОРИЯМ").FontSize(11).Bold().FontColor(primaryColor);
                        col.Item().Border(1).BorderColor(borderColor).Padding(10).Column(labChartCol =>
                        {
                            var maxLab = statistics.Laboratories.Max(x => x.CompletedTests);
                            var labs = statistics.Laboratories.Take(8).ToList();
                            
                            foreach (var lab in labs)
                            {
                                var barWidth = maxLab > 0 ? (int)(lab.CompletedTests * 100.0 / maxLab) : 0;
                                var barWidthBad = maxLab > 0 ? (int)(lab.RejectedTests * 100.0 / maxLab) : 0;
                                
                                labChartCol.Item().Row(r =>
                                {
                                    r.AutoItem().PaddingRight(5).Text(lab.LaboratoryName).FontSize(8);
                                    r.RelativeItem().Height(14).LineVertical(0.5f).LineColor(borderColor);
                                    if (barWidthBad > 0)
                                        r.RelativeItem().Width(barWidthBad).Height(14).Background(accentRed);
                                    if (barWidth > barWidthBad)
                                        r.RelativeItem().Width(barWidth - barWidthBad).Height(14).Background(primaryColor);
                                    r.AutoItem().PaddingLeft(3).Text($"{lab.CompletedTests}").FontSize(8).Bold();
                                    r.AutoItem().Text($" ({lab.RejectedTests} брак)").FontSize(7).FontColor(accentRed);
                                });
                            }
                        });
                    }

                    if (statistics.WireCodes?.Count > 0)
                    {
                        col.Item().Text("ПО КОДАМ ПРОВОЛОКИ").FontSize(11).Bold().FontColor(primaryColor);
                        col.Item().Border(1).BorderColor(borderColor).Padding(10).Column(wireChartCol =>
                        {
                            var maxWire = statistics.WireCodes.Max(x => x.CompletedTests);
                            var wires = statistics.WireCodes.Take(8).ToList();
                            
                            foreach (var wire in wires)
                            {
                                var barWidth = maxWire > 0 ? (int)(wire.CompletedTests * 100.0 / maxWire) : 0;
                                var barWidthBad = maxWire > 0 ? (int)(wire.RejectedTests * 100.0 / maxWire) : 0;
                                
                                wireChartCol.Item().Row(r =>
                                {
                                    r.AutoItem().PaddingRight(5).Text(wire.WireCode).FontSize(8);
                                    r.RelativeItem().Height(14).LineVertical(0.5f).LineColor(borderColor);
                                    if (barWidthBad > 0)
                                        r.RelativeItem().Width(barWidthBad).Height(14).Background(accentRed);
                                    if (barWidth > barWidthBad)
                                        r.RelativeItem().Width(barWidth - barWidthBad).Height(14).Background(primaryColor);
                                    r.AutoItem().PaddingLeft(3).Text($"{wire.CompletedTests}").FontSize(8).Bold();
                                    r.AutoItem().Text($" ({wire.RejectedTests}.)").FontSize(7).FontColor(accentRed);
                                });
                            }
});
                    }
                });

                page.Footer().AlignCenter().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
            });

            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(25);

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(CompanyName).FontSize(11).Bold().FontColor(primaryColor);
                    col.Item().AlignCenter().Text("СТАТИСТИКА ПРОБЛЕМ").FontSize(12).Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    if (statistics.ParameterViolations?.Count > 0)
                    {
                        col.Item().Text("НАРУШЕНИЯ ПАРАМЕТРОВ").FontSize(10).Bold().FontColor(accentRed);
                        col.Item().Border(1).BorderColor(borderColor).Padding(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(headerBg).Padding(4).Text("Параметр").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Единица").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Нарушений").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("%").FontColor(headerText).FontSize(8).Bold();
                            });
                            foreach (var v in statistics.ParameterViolations.Take(10))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(v.ParameterName).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(v.Unit ?? "—").FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(v.OutOfSpecCount.ToString()).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text($"{v.SharePercent}%").FontSize(8).FontColor(accentRed);
                            }
                        });
                    }

                    if (statistics.RejectReasons?.Count > 0)
                    {
                        col.Item().Text("ПРИЧИНЫ БРАКА").FontSize(10).Bold().FontColor(accentOrange);
                        col.Item().Border(1).BorderColor(borderColor).Padding(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(4);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(headerBg).Padding(4).Text("Причина").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Количество").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("%").FontColor(headerText).FontSize(8).Bold();
                            });
                            foreach (var r in statistics.RejectReasons.Take(10))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(r.Reason).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(r.Count.ToString()).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text($"{r.SharePercent}%").FontSize(8);
                            }
                        });
                    }

                    if (statistics.AssistantCycles?.Count > 0)
                    {
                        col.Item().Text("РАБОТА ЛАБОРАНТОВ").FontSize(10).Bold().FontColor(primaryColor);
                        col.Item().Border(1).BorderColor(borderColor).Padding(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(headerBg).Padding(4).Text("Лаборант").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Лаборатория").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Выполнено").FontColor(headerText).FontSize(8).Bold();
                                h.Cell().Background(headerBg).Padding(4).Text("Ср.цикл").FontColor(headerText).FontSize(8).Bold();
                            });
                            foreach (var a in statistics.AssistantCycles.Take(10))
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(a.AssistantName).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(a.LaboratoryName).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text(a.CompletedTests.ToString()).FontSize(8);
                                table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(4).Text($"{a.AvgCycleHours} ч").FontSize(8);
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();

            return Result.Success(new ReportFile
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"statistics-{fromUtc:yyyy-MM-dd}-{toUtc:yyyy-MM-dd}.pdf"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации отчета: {ex.Message}");
        }
    }

    public async Task<Result<ReportFile>> GenerateMonthlyJournalExcelAsync(int year, int month, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);
        return await GenerateDetailedJournalExcelAsync(from, to, null, null, cancellationToken, $"journal-{year:D4}-{month:D2}.xlsx");
    }

    public async Task<Result<ReportFile>> GenerateDetailedJournalExcelAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        int? wireCodeId,
        CancellationToken cancellationToken)
    {
        return await GenerateDetailedJournalExcelAsync(fromUtc, toUtc, laboratoryId, wireCodeId, cancellationToken, null);
    }

    private async Task<Result<ReportFile>> GenerateDetailedJournalExcelAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        int? wireCodeId,
        CancellationToken cancellationToken,
        string? fixedFileName)
    {
        try
        {
            var query = dbContext.TestResults
                .AsNoTracking()
                .Include(x => x.WireCode)
                .Include(x => x.Assistant)
                .Include(x => x.Laboratory)
                .Include(x => x.Customer)
                .Include(x => x.Reject)
                .Include(x => x.Values)
                    .ThenInclude(v => v.Parameter)
                .Where(x => x.Date >= fromUtc && x.Date < toUtc);

            if (laboratoryId.HasValue)
                query = query.Where(x => x.LaboratoryId == laboratoryId.Value);
            if (wireCodeId.HasValue)
                query = query.Where(x => x.WireCodeId == wireCodeId.Value);

            var items = await query
                .OrderBy(x => x.Date)
                .ToListAsync(cancellationToken);

            // Get unique parameters present in these results
            var parameters = items
                .SelectMany(x => x.Values)
                .Select(v => v.Parameter)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderBy(p => p.Name)
                .ToList();

            var completedItems = items.Where(x => x.Status == TestResultStatus.Completed).ToList();
            var rejectedCount = items.Count(x => x.Reject != null);
            var acceptedCount = items.Count(x => x.FinalProduct != null || (x.Status == TestResultStatus.Completed && x.Reject == null));
            var inProgressCount = items.Count(x => x.Status == TestResultStatus.InProgress);

            using var workbook = new XLWorkbook();
            var ws = workbook.AddWorksheet("Журнал испытаний");

            var headerColor = XLColor.FromHtml("#2c5282");
            var headerTextColor = XLColor.White;
            var altRowColor = XLColor.FromHtml("#f7fafc");
            var borderColor = XLColor.FromHtml("#cbd5e0");
            var successColor = XLColor.FromHtml("#c6f6d5");
            var errorColor = XLColor.FromHtml("#fed7d7");
            var warningColor = XLColor.FromHtml("#feebc8");

            var totalCols = 9 + parameters.Count;

            // Report Header
            var row = 1;
            ws.Cell(row, 1).Value = CompanyName;
            ws.Range(row, 1, row, totalCols).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 14;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            ws.Cell(row, 1).Value = ReportTitle;
            ws.Range(row, 1, row, totalCols).Merge();
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Font.Italic = true;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            ws.Cell(row, 1).Value = $"Период: {fromUtc:dd.MM.yyyy} — {toUtc:dd.MM.yyyy}";
            ws.Range(row, 1, row, totalCols).Merge();
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row += 2;

            // Summary Table
            ws.Cell(row, 1).Value = "СВОДКА";
            ws.Range(row, 1, row, 2).Merge();
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#edf2f7");
            row++;

            void AddSummaryRow(string label, int value, XLColor? bgColor = null)
            {
                ws.Cell(row, 1).Value = label;
                ws.Cell(row, 2).Value = value;
                if (bgColor != null) ws.Cell(row, 2).Style.Fill.BackgroundColor = bgColor;
                row++;
            }

            AddSummaryRow("Всего испытаний:", items.Count);
            AddSummaryRow("Принято:", acceptedCount, successColor);
            AddSummaryRow("Забраковано:", rejectedCount, errorColor);
            AddSummaryRow("В работе:", inProgressCount, warningColor);
            row += 2;

            // Table Headers
            var baseHeaders = new[] { "№", "Дата", "Партия", "Код проволоки", "Маркировка", "Потребитель", "Лаборатория", "Лаборант", "Статус" };
            for (var c = 0; c < baseHeaders.Length; c++)
            {
                var cell = ws.Cell(row, c + 1);
                cell.Value = baseHeaders[c];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Font.FontColor = headerTextColor;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Parameter Headers
            for (var p = 0; p < parameters.Count; p++)
            {
                var cell = ws.Cell(row, baseHeaders.Length + p + 1);
                var unit = string.IsNullOrWhiteSpace(parameters[p].Unit) ? "" : $", {parameters[p].Unit}";
                cell.Value = $"{parameters[p].Name}{unit}";
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#4a5568");
                cell.Style.Font.FontColor = headerTextColor;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Reject Reason Header
            var rejectReasonCol = baseHeaders.Length + parameters.Count + 1;
            ws.Cell(row, rejectReasonCol).Value = "Причина брака";
            ws.Cell(row, rejectReasonCol).Style.Font.Bold = true;
            ws.Cell(row, rejectReasonCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#718096");
            ws.Cell(row, rejectReasonCol).Style.Font.FontColor = headerTextColor;

            row++;
            var dataStartRow = row;

            // Freeze headers
            ws.SheetView.FreezeRows(dataStartRow - 1);

            // Data Rows
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var col = 1;

                ws.Cell(row, col++).Value = item.Id;
                ws.Cell(row, col).Value = item.Date;
                ws.Cell(row, col++).Style.DateFormat.Format = "dd.mm.yyyy HH:mm";
                ws.Cell(row, col++).Value = item.BatchNumber;
                ws.Cell(row, col++).Value = item.WireCode.Code;
                ws.Cell(row, col++).Value = item.WireCode.Marking;
                ws.Cell(row, col++).Value = item.Customer?.Name ?? "—";
                ws.Cell(row, col++).Value = item.Laboratory?.Name ?? "—";
                ws.Cell(row, col++).Value = item.Assistant.FullName;

                var statusCell = ws.Cell(row, col++);
                if (item.Reject != null)
                {
                    statusCell.Value = "Забраковано";
                    statusCell.Style.Fill.BackgroundColor = errorColor;
                }
                else if (item.Status == TestResultStatus.Completed)
                {
                    statusCell.Value = "Принято";
                    statusCell.Style.Fill.BackgroundColor = successColor;
                }
                else
                {
                    statusCell.Value = "В работе";
                    statusCell.Style.Fill.BackgroundColor = warningColor;
                }

                // Values
                foreach (var param in parameters)
                {
                    var val = item.Values.FirstOrDefault(v => v.ParameterId == param.Id);
                    if (val != null)
                    {
                        ws.Cell(row, col).Value = val.Value;
                        ws.Cell(row, col).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }
                    col++;
                }

                // Reject Reason
                if (item.Reject != null)
                {
                    ws.Cell(row, col).Value = item.Reject.Reason;
                }

                if (i % 2 == 1)
                {
                    ws.Range(row, 1, row, totalCols + 1).Style.Fill.BackgroundColor = altRowColor;
                }

                row++;
            }

            // Styling
            var lastCol = totalCols + 1;
            var dataRange = ws.Range(dataStartRow - 1, 1, row - 1, lastCol);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = borderColor;
            dataRange.Style.Border.InsideBorderColor = borderColor;

            ws.Columns().AdjustToContents();
            ws.Column(2).Width = 18; // Date
            ws.Column(6).Width = 20; // Customer
            ws.Column(lastCol).Width = 30; // Reject Reason

            await using var ms = new MemoryStream();
            workbook.SaveAs(ms);

            var fileName = fixedFileName ?? $"journal-{fromUtc:yyyy-MM-dd}-{toUtc:yyyy-MM-dd}.xlsx";
            return Result.Success(new ReportFile
            {
                Content = ms.ToArray(),
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации отчета: {ex.Message}");
        }
    }

    public async Task<Result<ReportFile>> GenerateBatchCertificatePdfAsync(int testResultId, CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var testResult = await dbContext.TestResults
                .AsNoTracking()
                .Include(x => x.WireCode)
                .Include(x => x.Assistant)
                .Include(x => x.Laboratory)
                .Include(x => x.Values)
                    .ThenInclude(v => v.Parameter)
                .FirstOrDefaultAsync(x => x.Id == testResultId, cancellationToken);

            if (testResult is null)
                return Result.Failure<ReportFile>("Результат испытания не найден.");

            var isAccepted = await dbContext.FinalProducts.AnyAsync(x => x.TestResultId == testResultId, cancellationToken);
            var rejectReason = isAccepted
                ? null
                : await dbContext.Rejects
                    .Where(x => x.TestResultId == testResultId)
                    .Select(x => x.Reason)
                    .FirstOrDefaultAsync(cancellationToken);

            var primaryColor = Color.FromHex("#2c5282");
            var headerBg = Color.FromHex("#2c5282");
            var headerText = Colors.White;
            var borderColor = Color.FromHex("#cbd5e0");

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Column(col =>
                    {
                        col.Item().PaddingBottom(8).AlignCenter().Text(CompanyName).FontSize(14).Bold().FontColor(primaryColor);
                        col.Item().AlignCenter().Text(ReportTitle).FontSize(10).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(4).AlignCenter().Text("Сертификат качества").FontSize(16).Bold();
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(12);

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("№ протокола:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(testResult.Id.ToString()).FontSize(12).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Партия:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(testResult.BatchNumber).FontSize(12).Bold();
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Дата:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(testResult.Date.ToString("dd.MM.yyyy HH:mm")).FontSize(12);
                            });
                        });

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Код проволоки:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text($"{testResult.WireCode.Code} ({testResult.WireCode.Marking})").FontSize(11);
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Лаборант:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(testResult.Assistant.FullName).FontSize(11);
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Лаборатория:").FontSize(9).FontColor(Colors.Grey.Darken1);
                                c.Item().Text(testResult.Laboratory?.Name ?? "—").FontSize(11);
                            });
                        });

                        col.Item().PaddingTop(8).Border(1).BorderColor(borderColor).Padding(8).Column(statusCol =>
                        {
                            statusCol.Item().Text(isAccepted ? "РЕЗУЛЬТАТ: ПРИНЯТО" : "РЕЗУЛЬТАТ: ЗАБРАКОВАНО")
                                .FontSize(14).Bold().FontColor(isAccepted ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            if (!isAccepted && !string.IsNullOrWhiteSpace(rejectReason))
                                statusCol.Item().PaddingTop(4).Text($"Причина: {rejectReason}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });

                        col.Item().PaddingTop(8).Text("Результаты измерений").FontSize(12).Bold();

                        var values = testResult.Values.OrderBy(x => x.Parameter.Name).ToList();
                        if (values.Count > 0)
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background(headerBg).Padding(6).Text("Параметр").FontColor(headerText).Bold();
                                    h.Cell().Background(headerBg).Padding(6).Text("Значение").FontColor(headerText).Bold();
                                    h.Cell().Background(headerBg).Padding(6).Text("Ед.").FontColor(headerText).Bold();
                                });

                                foreach (var v in values)
                                {
                                    var unit = string.IsNullOrWhiteSpace(v.Parameter.Unit) ? "—" : v.Parameter.Unit;
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text(v.Parameter.Name);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text(v.Value);
                                    table.Cell().BorderBottom(0.5f).BorderColor(borderColor).Padding(6).Text(unit);
                                }
                            });
                        }
                        else
                        {
                            col.Item().Text("Нет данных измерений").FontColor(Colors.Grey.Medium);
                        }
                    });

                    page.Footer().AlignCenter().Column(c =>
                    {
                        c.Item().Text($"Сформировано: {DateTime.UtcNow:dd.MM.yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                        c.Item().Text("Сертификат качества — ОАО «БМЗ»").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();

            return Result.Success(new ReportFile
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"certificate-{testResultId}-{testResult.BatchNumber}.pdf"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации отчета: {ex.Message}");
        }
    }
}
