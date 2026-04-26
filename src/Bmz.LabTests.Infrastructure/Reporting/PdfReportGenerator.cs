using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Application.Abstractions.Testing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Bmz.LabTests.Infrastructure.Reporting;

public sealed class PdfReportGenerator
{
    public byte[] GenerateStatisticsPdf(StatisticsPdfData data, StatisticsTrendGroupBy groupBy)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var groupByName = groupBy switch
        {
            StatisticsTrendGroupBy.Day => "по дням",
            StatisticsTrendGroupBy.Week => "по неделям",
            StatisticsTrendGroupBy.Month => "по месяцам",
            _ => "по дням"
        };

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().PaddingBottom(5).AlignCenter().Text(ReportConstants.CompanyName).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text(ReportConstants.ReportTitle).FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(3).AlignCenter().Text("ОТЧЕТ ПО СТАТИСТИКЕ").FontSize(14).Bold();
                });

                page.Content().Column(col =>
                {
                    col.Spacing(15);

                    col.Item().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(summary =>
                    {
                        summary.Item().Text("ПЕРИОД И ФИЛЬТРЫ").FontSize(10).Bold().FontColor(Colors.Blue.Darken2);
                        summary.Item().PaddingTop(5).Text($"Лаборатория: {data.LabName}").FontSize(10);
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ВСЕГО ИСПЫТАНИЙ").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(data.TotalTests.ToString()).FontSize(24).Bold().FontColor(Colors.Blue.Darken2);
                        });
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАВЕРШЕНО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(data.CompletedTests.ToString()).FontSize(24).Bold().FontColor(Colors.Green.Medium);
                        });
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("В РАБОТЕ").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text(data.InProgressTests.ToString()).FontSize(24).Bold().FontColor(Colors.Blue.Medium);
                        });
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ПРИНЯТО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{data.AcceptedTests} ({data.AcceptanceRatePercent}%)").FontSize(20).Bold().FontColor(Colors.Green.Medium);
                        });
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("ЗАБРАКОВАНО").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{data.RejectedTests} ({data.RejectRatePercent}%)").FontSize(20).Bold().FontColor(Colors.Red.Medium);
                        });
                        row.RelativeItem().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(c =>
                        {
                            c.Item().Text("СР. ВРЕМЯ ЦИКЛА").FontSize(9).Bold().FontColor(Colors.Grey.Darken1);
                            c.Item().PaddingTop(3).Text($"{data.AvgCycleHours} ч").FontSize(20).Bold();
                        });
                    });

                    col.Item().PaddingTop(10).Text("ДИНАМИКА ИСПЫТАНИЙ").FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Border(1).BorderColor(ReportConstants.BorderColor).Padding(10).Column(chartCol =>
                    {
                        if (data.Trends.Count > 0)
                        {
                            var maxVal = data.Trends.Max(x => x.Total);
                            foreach (var point in data.Trends)
                            {
                                var barWidth = maxVal > 0 ? (int)(point.Total * 30.0 / maxVal) : 0;
                                var label = point.Period.ToString("dd.MM");
                                chartCol.Item().Row(r =>
                                {
                                    r.AutoItem().Text(label).FontSize(8);
                                    r.AutoItem().Width(barWidth).Height(10).Background(Colors.Blue.Darken2);
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
                            labCol.Item().Text("ПО ЛАБОРАТОРИЯМ").FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                            labCol.Item().Border(1).BorderColor(ReportConstants.BorderColor).Padding(5).Table(table =>
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
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Лаборатория").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Всего").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Брак").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("%").FontColor(Colors.White).FontSize(8).Bold();
                                });
                                foreach (var lab in data.LabStats.Take(8))
                                {
                                    var rate = lab.Completed == 0 ? 0 : Math.Round((decimal)lab.Rejected * 100 / lab.Completed, 1);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(lab.Name).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(lab.Completed.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(lab.Rejected.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text($"{rate}%").FontSize(8).FontColor(rate > 10 ? Colors.Red.Medium : Colors.Black);
                                }
                            });
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Column(wireCol =>
                        {
                            wireCol.Item().Text("ПО КОДАМ ПРОВОЛОКИ").FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                            wireCol.Item().Border(1).BorderColor(ReportConstants.BorderColor).Padding(5).Table(table =>
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
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Код").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Всего").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("Брак").FontColor(Colors.White).FontSize(8).Bold();
                                    h.Cell().Background(ReportConstants.HeaderBg).Padding(4).Text("%").FontColor(Colors.White).FontSize(8).Bold();
                                });
                                foreach (var wire in data.WireStats.OrderByDescending(x => x.Completed).Take(8))
                                {
                                    var rate = wire.Completed == 0 ? 0 : Math.Round((decimal)wire.Rejected * 100 / wire.Completed, 1);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(wire.Code).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(wire.Completed.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text(wire.Rejected.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor(ReportConstants.BorderColor).Padding(4).Text($"{rate}%").FontSize(8).FontColor(rate > 10 ? Colors.Red.Medium : Colors.Black);
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
    }
}