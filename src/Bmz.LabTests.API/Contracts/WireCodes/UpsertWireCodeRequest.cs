namespace Bmz.LabTests.API.Contracts.WireCodes;

public sealed class UpsertWireCodeRequest
{
    public string Code { get; set; } = string.Empty;

    public string Marking { get; set; } = string.Empty;

    public decimal Diameter { get; set; }
}
