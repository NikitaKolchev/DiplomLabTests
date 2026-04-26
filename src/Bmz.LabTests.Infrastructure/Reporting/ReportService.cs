using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Infrastructure.Persistence;

namespace Bmz.LabTests.Infrastructure.Reporting;

public sealed class ReportService(
    ExcelReportGenerator excelGenerator,
    PdfReportGenerator pdfGenerator) : IReportService
{
    public Task<Result<ReportFile>> GenerateMonthlyJournalExcelAsync(int year, int month, CancellationToken cancellationToken)
        => excelGenerator.GenerateMonthlyJournalExcelAsync(year, month, cancellationToken);

    public Task<Result<ReportFile>> GenerateDetailedJournalExcelAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        int? wireCodeId,
        CancellationToken cancellationToken)
        => excelGenerator.GenerateDetailedJournalExcelAsync(fromUtc, toUtc, laboratoryId, wireCodeId, cancellationToken);

    public Task<Result<ReportFile>> GenerateStatisticsPdfAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken)
        => pdfGenerator.GenerateStatisticsPdfAsync(fromUtc, toUtc, laboratoryId, groupBy, null, cancellationToken);

    public Task<Result<ReportFile>> GenerateStatisticsPdfWithChartsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        StatisticsResponseDto statistics,
        CancellationToken cancellationToken)
        => pdfGenerator.GenerateStatisticsPdfAsync(fromUtc, toUtc, laboratoryId, groupBy, statistics, cancellationToken);

    public Task<Result<ReportFile>> GenerateBatchCertificatePdfAsync(int testResultId, CancellationToken cancellationToken)
        => pdfGenerator.GenerateBatchCertificatePdfAsync(testResultId, cancellationToken);
}
