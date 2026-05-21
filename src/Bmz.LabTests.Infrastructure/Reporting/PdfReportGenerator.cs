using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bmz.LabTests.Infrastructure.Reporting;

/// <summary>
/// Генератор отчетов в формате PDF.
/// Использует библиотеку QuestPDF для создания документов с богатым визуальным оформлением (графики, таблицы, карточки).
/// </summary>
public sealed class PdfReportGenerator(ApplicationDbContext dbContext)
{
    private const string CompanyName = "ОАО «БМЗ» — Управляющая компания холдинга «БМК»";
    private const string DepartmentName = "Сталепроволочный цех №1 — Испытательная лаборатория";
    private const string ReportTitle = "Система регистрации лабораторных испытаний";

    /// <summary>
    /// Генерирует статистический отчет с показателями качества, динамикой испытаний и анализом брака.
    /// </summary>
    public async Task<Result<ReportFile>> GenerateStatisticsPdfAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        StatisticsResponseDto? statistics,
        CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            if (statistics == null)
            {
                // Fallback to extraction if not provided
                var extractor = new ReportDataExtractor(dbContext);
                var data = await extractor.ExtractStatisticsDataAsync(fromUtc, toUtc, laboratoryId, groupBy, cancellationToken);
                
                // Map to DTO format for consistent rendering
                statistics = new StatisticsResponseDto(
                    new StatisticsOverviewDto(
                        data.TotalTests,
                        data.CompletedTests,
                        data.InProgressTests,
                        data.RejectedTests,
                        data.RejectRatePercent,
                        data.AcceptanceRatePercent,
                        data.AvgCycleHours),
                    data.LabStats.Select(x => new StatisticsLaboratorySliceDto(0, x.Name, x.Completed, x.Rejected, 0, 0)).ToArray(),
                    data.WireStats.Select(x => new StatisticsWireCodeSliceDto(0, x.Code, x.Completed, x.Rejected, 0)).ToArray(),
                    [],
                    data.Trends.Select(x => new StatisticsTrendPointDto(x.Period, x.Total, x.Total, 0)).ToArray(),
                    [],
                    []);
            }

            var labName = laboratoryId.HasValue
                ? await dbContext.Laboratories.Where(x => x.Id == laboratoryId.Value).Select(x => x.Name).FirstOrDefaultAsync(cancellationToken)
                : "Все лаборатории";

            var logoBytes = GetLogo();
            var primaryColor = Color.FromHex(ReportConstants.PrimaryColor);
            var headerBg = Color.FromHex(ReportConstants.HeaderBg);
            var borderColor = Color.FromHex(ReportConstants.BorderColor);

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        if (logoBytes != null)
                        {
                            row.ConstantItem(60).Image(logoBytes);
                        }

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(CompanyName).FontSize(12).Bold().FontColor(primaryColor);
                            col.Item().Text(DepartmentName).FontSize(10).FontColor(Colors.Grey.Darken2);
                            col.Item().Text(ReportTitle).FontSize(8).Italic().FontColor(Colors.Grey.Medium);
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text("СТАТИСТИЧЕСКИЙ ОТЧЕТ").FontSize(14).Bold();
                            col.Item().Text($"{fromUtc:dd.MM.yyyy} — {toUtc:dd.MM.yyyy}").FontSize(10);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(15);

                        // Filters summary
                        col.Item().PaddingTop(10).BorderBottom(1).BorderColor(borderColor).PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("Фильтр: ").Bold();
                                t.Span(labName);
                            });
                            row.RelativeItem().AlignRight().Text(t =>
                            {
                                t.Span("Группировка: ").Bold();
                                t.Span(groupBy switch
                                {
                                    StatisticsTrendGroupBy.Day => "По дням",
                                    StatisticsTrendGroupBy.Week => "По неделям",
                                    StatisticsTrendGroupBy.Month => "По месяцам",
                                    _ => "По дням"
                                });
                            });
                        });

                        // Key metrics cards
                        col.Item().Row(row =>
                        {
                            row.Spacing(10);
                            row.RelativeItem().Component(new MetricCard("ВСЕГО", statistics.Overview.TotalTests.ToString(), ReportConstants.AccentColor));
                            row.RelativeItem().Component(new MetricCard("ПРИНЯТО", $"{statistics.Overview.CompletedTests - statistics.Overview.RejectedTests}", ReportConstants.SuccessColor));
                            row.RelativeItem().Component(new MetricCard("БРАК", statistics.Overview.RejectedTests.ToString(), ReportConstants.DangerColor));
                            row.RelativeItem().Component(new MetricCard("% БРАКА", $"{statistics.Overview.RejectRatePercent}%", statistics.Overview.RejectRatePercent > 5 ? ReportConstants.DangerColor : ReportConstants.SuccessColor));
                        });

                        // Trends section
                        if (statistics.Trends.Any())
                        {
                            col.Item().Column(trendCol =>
                            {
                                trendCol.Item().Text("ДИНАМИКА ИСПЫТАНИЙ").FontSize(11).Bold().FontColor(primaryColor);
                                trendCol.Item().PaddingTop(5).Height(150).Border(1).BorderColor(borderColor).Padding(10).Row(row =>
                                {
                                    var maxVal = statistics.Trends.Max(x => x.TotalTests);
                                    if (maxVal == 0) maxVal = 1;

                                    foreach (var point in statistics.Trends.TakeLast(15))
                                    {
                                        row.RelativeItem().Column(c =>
                                        {
                                            c.Item().Extend().AlignBottom().Row(r =>
                                            {
                                                r.RelativeItem().Height((float)point.TotalTests * 100 / maxVal).Background(primaryColor);
                                            });
                                            c.Item().AlignCenter().Text(point.PeriodStartUtc.ToString("dd.MM")).FontSize(7);
                                        });
                                    }
                                });
                            });
                        }

                        // Detailed tables
                        col.Item().Row(row =>
                        {
                            row.Spacing(20);

                            // Lab stats
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ПО ЛАБОРАТОРИЯМ").FontSize(11).Bold().FontColor(primaryColor);
                                c.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(3);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                    });
                                    table.Header(h =>
                                    {
                                        h.Cell().Element(HeaderStyle).Text("Лаборатория");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("Всего");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("Брак");
                                    });
                                    foreach (var lab in statistics.Laboratories.Take(10))
                                    {
                                        table.Cell().Element(CellStyle).Text(lab.LaboratoryName);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(lab.CompletedTests.ToString());
                                        table.Cell().Element(CellStyle).AlignCenter().Text(lab.RejectedTests.ToString()).FontColor(lab.RejectedTests > 0 ? Color.FromHex(ReportConstants.DangerColor) : Colors.Black);
                                    }
                                });
                            });

                            // Wire stats
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("ТОП-10 БРАКА (КОДЫ)").FontSize(11).Bold().FontColor(primaryColor);
                                c.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(3);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                    });
                                    table.Header(h =>
                                    {
                                        h.Cell().Element(HeaderStyle).Text("Код");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("Всего");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("Брак");
                                    });
                                    foreach (var wire in statistics.WireCodes.OrderByDescending(x => x.RejectRatePercent).Take(10))
                                    {
                                        table.Cell().Element(CellStyle).Text(wire.WireCode);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(wire.CompletedTests.ToString());
                                        table.Cell().Element(CellStyle).AlignCenter().Text(wire.RejectedTests.ToString()).FontColor(wire.RejectedTests > 0 ? Color.FromHex(ReportConstants.DangerColor) : Colors.Black);
                                    }
                                });
                            });
                        });

                        // Reject reasons if available
                        if (statistics.RejectReasons.Any())
                        {
                            col.Item().Column(c =>
                            {
                                c.Item().Text("ПРИЧИНЫ БРАКОВКИ").FontSize(11).Bold().FontColor(primaryColor);
                                c.Item().PaddingTop(5).Table(table =>
                                {
                                    table.ColumnsDefinition(cd =>
                                    {
                                        cd.RelativeColumn(5);
                                        cd.RelativeColumn(1);
                                        cd.RelativeColumn(1);
                                    });
                                    table.Header(h =>
                                    {
                                        h.Cell().Element(HeaderStyle).Text("Причина");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("Кол-во");
                                        h.Cell().Element(HeaderStyle).AlignCenter().Text("%");
                                    });
                                    foreach (var reason in statistics.RejectReasons.Take(5))
                                    {
                                        table.Cell().Element(CellStyle).Text(reason.Reason);
                                        table.Cell().Element(CellStyle).AlignCenter().Text(reason.Count.ToString());
                                        table.Cell().Element(CellStyle).AlignCenter().Text($"{reason.SharePercent}%");
                                    }
                                });
                            });
                        }
                    });

                    page.Footer().Column(f =>
                    {
                        f.Item().BorderTop(1).BorderColor(borderColor).PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).FontColor(Color.FromHex(ReportConstants.MutedColor));
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Страница ").FontSize(8);
                                x.CurrentPageNumber().FontSize(8);
                                x.Span(" из ").FontSize(8);
                                x.TotalPages().FontSize(8);
                            });
                        });
                    });
                });
            }).GeneratePdf();

            return Result.Success(new ReportFile
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"statistics-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.pdf"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации PDF-отчета: {ex.Message}");
        }
    }

    /// <summary>
    /// Создает официальный сертификат испытаний для конкретной партии проволоки.
    /// </summary>
    public async Task<Result<ReportFile>> GenerateBatchCertificatePdfAsync(int testResultId, CancellationToken cancellationToken)
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var item = await dbContext.TestResults
                .AsNoTracking()
                .Include(x => x.WireCode)
                .Include(x => x.Assistant)
                .Include(x => x.Laboratory)
                .Include(x => x.Customer)
                .Include(x => x.Values).ThenInclude(v => v.Parameter)
                .Include(x => x.Reject)
                .FirstOrDefaultAsync(x => x.Id == testResultId, cancellationToken);

            if (item == null) return Result.Failure<ReportFile>("Испытание не найдено");

            var logoBytes = GetLogo();
            var primaryColor = Color.FromHex(ReportConstants.PrimaryColor);
            var borderColor = Color.FromHex(ReportConstants.BorderColor);

            var bytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        if (logoBytes != null)
                        {
                            row.ConstantItem(70).Image(logoBytes);
                        }

                        row.RelativeItem().PaddingLeft(10).Column(col =>
                        {
                            col.Item().Text(CompanyName).FontSize(12).Bold().FontColor(primaryColor);
                            col.Item().Text(DepartmentName).FontSize(10);
                            col.Item().Text("247210, Гомельская обл., г. Жлобин, ул. Промышленная, 37").FontSize(8);
                        });

                        row.ConstantItem(150).AlignRight().Column(col =>
                        {
                            col.Item().Border(1).Padding(5).AlignCenter().Column(c =>
                            {
                                c.Item().Text("СЕРТИФИКАТ").FontSize(10).Bold();
                                c.Item().Text("КАЧЕСТВА").FontSize(10).Bold();
                                c.Item().Text($"№ {item.Id:D6}").FontSize(12).Bold().FontColor(primaryColor);
                            });
                        });
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        col.Spacing(10);

                        // Main info table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(1);
                                cd.RelativeColumn(2);
                            });

                            table.Cell().Element(LabelStyle).Text("Наименование продукции:");
                            table.Cell().Element(ValueStyle).Text($"Проволока стальная {item.WireCode.Code}");

                            table.Cell().Element(LabelStyle).Text("Номер партии:");
                            table.Cell().Element(ValueStyle).Text(item.BatchNumber).Bold();

                            table.Cell().Element(LabelStyle).Text("Дата изготовления/испытания:");
                            table.Cell().Element(ValueStyle).Text(item.Date.ToString("dd.MM.yyyy"));

                            table.Cell().Element(LabelStyle).Text("Потребитель:");
                            table.Cell().Element(ValueStyle).Text(item.Customer?.Name ?? "Внутреннее перемещение");
                        });

                        col.Item().PaddingTop(15).Text("РЕЗУЛЬТАТЫ ИСПЫТАНИЙ").FontSize(12).Bold().AlignCenter();

                        // Results table
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(cd =>
                            {
                                cd.RelativeColumn(4);
                                cd.RelativeColumn(2);
                                cd.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderStyle).Text("Наименование показателя");
                                h.Cell().Element(HeaderStyle).AlignCenter().Text("Ед. изм.");
                                h.Cell().Element(HeaderStyle).AlignCenter().Text("Факт");
                            });

                            foreach (var val in item.Values)
                            {
                                table.Cell().Element(CellStyle).Text(val.Parameter.Name);
                                table.Cell().Element(CellStyle).AlignCenter().Text(val.Parameter.Unit ?? "—");
                                table.Cell().Element(CellStyle).AlignCenter().Text(val.Value).Bold();
                            }
                        });

                        // Decision
                        col.Item().PaddingTop(20).Border(1).Padding(10).Row(row =>
                        {
                            var isAccepted = item.Status == TestResultStatus.Completed && item.Reject == null;
                            row.RelativeItem().Text(t =>
                            {
                                t.Span("ЗАКЛЮЧЕНИЕ: ").Bold();
                                if (isAccepted)
                                {
                                    t.Span("Продукция соответствует требованиям нормативной документации и признана годной.").FontColor(Colors.Green.Darken2);
                                }
                                else
                                {
                                    t.Span("Продукция НЕ СООТВЕТСТВУЕТ требованиям. ").FontColor(Colors.Red.Medium);
                                    if (item.Reject != null) t.Span($"Причина: {item.Reject.Reason}");
                                }
                            });
                        });

                        // Signatures
                        col.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Лаборант").FontSize(9);
                                c.Item().PaddingTop(15).BorderTop(1).Text(item.Assistant.FullName).FontSize(10).Bold();
                            });
                            row.ConstantItem(50);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Начальник смены / Инженер").FontSize(9);
                                c.Item().PaddingTop(15).BorderTop(1).Text("____________________").FontSize(10);
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.Span("Настоящий сертификат подтверждает качество продукции ОАО «БМЗ»").FontSize(8).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf();

            return Result.Success(new ReportFile
            {
                Content = bytes,
                ContentType = "application/pdf",
                FileName = $"Certificate_{item.BatchNumber}_{item.Date:yyyyMMdd}.pdf"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации сертификата: {ex.Message}");
        }
    }

    private byte[]? GetLogo()
    {
        try
        {
            var searchPaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Bmz.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "frontend", "Bmz.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "frontend", "public", "Bmz-removebg-preview.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "frontend", "public", "favicon.png")
            };

            foreach (var path in searchPaths)
            {
                if (File.Exists(path)) return File.ReadAllBytes(path);
            }

            return null;
        }
        catch { return null; }
    }

    // Styles
    private IContainer HeaderStyle(IContainer container) => container
        .Background(Color.FromHex(ReportConstants.HeaderBg))
        .Padding(5)
        .DefaultTextStyle(x => x.FontColor(Colors.White).Bold().FontSize(9));

    private IContainer CellStyle(IContainer container) => container
        .BorderBottom(0.5f)
        .BorderColor(Color.FromHex(ReportConstants.BorderColor))
        .Padding(5)
        .DefaultTextStyle(x => x.FontSize(9));

    private IContainer LabelStyle(IContainer container) => container
        .PaddingVertical(3)
        .DefaultTextStyle(x => x.FontSize(10).FontColor(Color.FromHex(ReportConstants.MutedColor)));

    private IContainer ValueStyle(IContainer container) => container
        .PaddingVertical(3)
        .DefaultTextStyle(x => x.FontSize(10).Bold());

    private class MetricCard(string label, string value, string color) : IComponent
    {
        public void Compose(IContainer container)
        {
            container
                .Border(1)
                .BorderColor(Color.FromHex(ReportConstants.BorderColor))
                .Decoration(decoration =>
                {
                    decoration.Before().Height(3).Background(color);
                    decoration.Content().Padding(10).Column(c =>
                    {
                        c.Item().Text(label).FontSize(8).Bold().FontColor(Color.FromHex(ReportConstants.MutedColor));
                        c.Item().Text(value).FontSize(18).Bold().FontColor(color);
                    });
                });
        }
    }
}
