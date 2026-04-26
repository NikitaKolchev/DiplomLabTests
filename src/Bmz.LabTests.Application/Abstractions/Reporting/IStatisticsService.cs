using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Reporting;

public interface IStatisticsService
{
    Task<Result<StatisticsResponseDto>> GetAsync(
        DateTime fromUtc,
        DateTime toUtc,
        StatisticsTrendGroupBy groupBy,
        CancellationToken cancellationToken);
}

public enum StatisticsTrendGroupBy
{
    Day = 1,
    Week = 2,
    Month = 3
}

public sealed record StatisticsResponseDto(
    StatisticsOverviewDto Overview,
    IReadOnlyCollection<StatisticsLaboratorySliceDto> Laboratories,
    IReadOnlyCollection<StatisticsWireCodeSliceDto> WireCodes,
    IReadOnlyCollection<StatisticsParameterViolationDto> ParameterViolations,
    IReadOnlyCollection<StatisticsTrendPointDto> Trends,
    IReadOnlyCollection<StatisticsRejectReasonDto> RejectReasons,
    IReadOnlyCollection<StatisticsCycleByAssistantDto> AssistantCycles);

public sealed record StatisticsOverviewDto(
    int TotalTests,
    int CompletedTests,
    int InProgressTests,
    int RejectedTests,
    decimal RejectRatePercent,
    decimal AcceptanceRatePercent,
    decimal AvgCycleHours);

public sealed record StatisticsLaboratorySliceDto(
    int LaboratoryId,
    string LaboratoryName,
    int CompletedTests,
    int RejectedTests,
    decimal RejectRatePercent,
    decimal AvgCycleHours);

public sealed record StatisticsWireCodeSliceDto(
    int WireCodeId,
    string WireCode,
    int CompletedTests,
    int RejectedTests,
    decimal RejectRatePercent);

public sealed record StatisticsParameterViolationDto(
    int ParameterId,
    string ParameterName,
    string? Unit,
    int OutOfSpecCount,
    decimal SharePercent);

public sealed record StatisticsTrendPointDto(
    DateTime PeriodStartUtc,
    int TotalTests,
    int CompletedTests,
    int RejectedTests);

public sealed record StatisticsRejectReasonDto(
    string Reason,
    int Count,
    decimal SharePercent);

public sealed record StatisticsCycleByAssistantDto(
    int AssistantId,
    string AssistantName,
    string LaboratoryName,
    int CompletedTests,
    decimal AvgCycleHours);
