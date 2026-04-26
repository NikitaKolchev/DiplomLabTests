using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.TestResults;

public sealed record TestResultListItemDto(
    int Id,
    DateTime Date,
    DateTime UpdatedAtUtc,
    string BatchNumber,
    TestResultStatus Status,
    int WireCodeId,
    string WireCode,
    string Assistant,
    string RowVersion);

public sealed record PaginatedListDto<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record TestResultValueDto(int ParameterId, string Value);

public sealed record TestResultDetailsDto(
    int Id,
    DateTime Date,
    DateTime UpdatedAtUtc,
    int AssistantId,
    int WireCodeId,
    string BatchNumber,
    TestResultStatus Status,
    string RowVersion,
    IReadOnlyCollection<TestResultValueDto> Values);

public sealed record CreateTestResultDto(int AssistantId, int WireCodeId, string BatchNumber, int? CustomerId);

public sealed record CreatedTestResultDto(
    int Id,
    DateTime Date,
    DateTime UpdatedAtUtc,
    int WireCodeId,
    string BatchNumber,
    TestResultStatus Status,
    string RowVersion);

public sealed record SaveValueItemDto(int ParameterId, string Value);

public sealed record SaveTestValuesDto(string RowVersion, IReadOnlyCollection<SaveValueItemDto> Values);

public sealed record SavedTestResultDto(int Id, DateTime UpdatedAtUtc, string RowVersion);
