using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.API.Contracts.Parameters;

public sealed class UpsertParameterRequest
{
    public string Name { get; set; } = string.Empty;

    public ParameterDataType DataType { get; set; }

    public string? Unit { get; set; }
}
