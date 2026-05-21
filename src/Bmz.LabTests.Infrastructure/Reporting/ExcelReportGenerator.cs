using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;
using Bmz.LabTests.Infrastructure.Persistence;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Reporting;

/// <summary>
/// Генератор отчетов в формате Microsoft Excel (XLSX).
/// Использует библиотеку ClosedXML для формирования табличных журналов.
/// </summary>
public sealed class ExcelReportGenerator(ApplicationDbContext dbContext)
{
    /// <summary>
    /// Генерирует ежемесячный журнал за указанный год и месяц.
    /// </summary>
    public async Task<Result<ReportFile>> GenerateMonthlyJournalExcelAsync(int year, int month, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);
        return await GenerateDetailedJournalExcelAsync(from, to, null, null, cancellationToken);
    }

    /// <summary>
    /// Формирует подробный журнал испытаний с гибкой фильтрацией и форматированием.
    /// </summary>
    public async Task<Result<ReportFile>> GenerateDetailedJournalExcelAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        int? wireCodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var query = dbContext.TestResults
                .AsNoTracking()
                .Include(x => x.WireCode)
                .Include(x => x.Assistant)
                .Include(x => x.Laboratory)
                .Include(x => x.Values).ThenInclude(v => v.Parameter)
                .Where(x => x.Date >= fromUtc && x.Date < toUtc);

            if (laboratoryId.HasValue) query = query.Where(x => x.LaboratoryId == laboratoryId.Value);
            if (wireCodeId.HasValue) query = query.Where(x => x.WireCodeId == wireCodeId.Value);

            var data = await query.OrderByDescending(x => x.Date).ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Журнал испытаний");

            var primaryColor = XLColor.FromHtml(ReportConstants.PrimaryColor);
            var headerBg = XLColor.FromHtml(ReportConstants.HeaderBg);

            // Title and header
            worksheet.Cell(1, 1).Value = "ОАО «БМЗ» — Управляющая компания холдинга «БМК»";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 14;
            worksheet.Cell(1, 1).Style.Font.FontColor = primaryColor;
            worksheet.Range(1, 1, 1, 8).Merge();

            worksheet.Cell(2, 1).Value = "ЖУРНАЛ РЕГИСТРАЦИИ ЛАБОРАТОРНЫХ ИСПЫТАНИЙ";
            worksheet.Cell(2, 1).Style.Font.Bold = true;
            worksheet.Cell(2, 1).Style.Font.FontSize = 12;
            worksheet.Range(2, 1, 2, 8).Merge();

            worksheet.Cell(3, 1).Value = $"Период: {fromUtc:dd.MM.yyyy} — {toUtc:dd.MM.yyyy}";
            worksheet.Range(3, 1, 3, 8).Merge();

            // Table headers
            int headerRow = 5;
            worksheet.Cell(headerRow, 1).Value = "№";
            worksheet.Cell(headerRow, 2).Value = "Дата/Время";
            worksheet.Cell(headerRow, 3).Value = "Партия";
            worksheet.Cell(headerRow, 4).Value = "Код проволоки";
            worksheet.Cell(headerRow, 5).Value = "Лаборатория";
            worksheet.Cell(headerRow, 6).Value = "Лаборант";
            worksheet.Cell(headerRow, 7).Value = "Статус";
            worksheet.Cell(headerRow, 8).Value = "Результаты (Параметр: Значение)";

            var headerRange = worksheet.Range(headerRow, 1, headerRow, 8);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = headerBg;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                int row = i + headerRow + 1;
                
                worksheet.Cell(row, 1).Value = i + 1;
                worksheet.Cell(row, 2).Value = item.Date;
                worksheet.Cell(row, 2).Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                
                worksheet.Cell(row, 3).Value = item.BatchNumber;
                worksheet.Cell(row, 3).Style.Font.Bold = true;
                
                worksheet.Cell(row, 4).Value = item.WireCode.Code;
                worksheet.Cell(row, 5).Value = item.Laboratory.Name;
                worksheet.Cell(row, 6).Value = item.Assistant.FullName;
                
                var statusCell = worksheet.Cell(row, 7);
                statusCell.Value = item.Status.ToString();
                if (item.Status == TestResultStatus.Completed) statusCell.Style.Font.FontColor = XLColor.Green;
                
                var valuesString = string.Join("\n", item.Values.Select(v => $"{v.Parameter.Name}: {v.Value} {v.Parameter.Unit}"));
                worksheet.Cell(row, 8).Value = valuesString;
                worksheet.Cell(row, 8).Style.Alignment.WrapText = true;
            }

            var dataRange = worksheet.Range(headerRow, 1, headerRow + data.Count, 8);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            worksheet.Columns(1, 7).AdjustToContents();
            worksheet.Column(8).Width = 60;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return Result.Success(new ReportFile
            {
                Content = content,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"journal-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx"
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<ReportFile>($"Ошибка при генерации Excel-отчета: {ex.Message}");
        }
    }
}
