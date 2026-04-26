using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.Protocol;

public sealed record WireCodeBriefDto(int Id, string Code, string Marking, decimal Diameter);

public sealed record InputFieldDto(
    int ParameterId,
    string ParameterName,
    ParameterDataType DataType,
    string? Unit,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue);

public sealed record WireCodeInputSchemaDto(WireCodeBriefDto WireCode, IReadOnlyCollection<InputFieldDto> Fields);

public sealed record LimitDto(
    int Id,
    int WireCodeId,
    int ParameterId,
    string ParameterName,
    ParameterDataType DataType,
    string? Unit,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue);

public sealed record LimitUpsertItemDto(int ParameterId, bool IsRequired, decimal? MinValue, decimal? MaxValue);
