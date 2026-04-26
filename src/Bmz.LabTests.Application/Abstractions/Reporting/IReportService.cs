using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Reporting;

public interface IReportService
{
    Task<Result<ReportFile>> GenerateMonthlyJournalExcelAsync(int year, int month, CancellationToken cancellationToken);

    Task<Result<ReportFile>> GenerateDetailedJournalExcelAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        int? wireCodeId,
        CancellationToken cancellationToken);

    Task<Result<ReportFile>> GenerateStatisticsPdfAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken);

    Task<Result<ReportFile>> GenerateStatisticsPdfWithChartsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        int? laboratoryId,
        StatisticsTrendGroupBy groupBy,
        StatisticsResponseDto statistics,
        CancellationToken cancellationToken);

    Task<Result<ReportFile>> GenerateBatchCertificatePdfAsync(int testResultId, CancellationToken cancellationToken);
}
