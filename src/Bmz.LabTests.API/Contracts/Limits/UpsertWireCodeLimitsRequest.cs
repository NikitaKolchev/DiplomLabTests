namespace Bmz.LabTests.API.Contracts.Limits;

public sealed class UpsertWireCodeLimitsRequest
{
    public List<WireCodeLimitItemRequest> Items { get; set; } = [];
}

public sealed class WireCodeLimitItemRequest
{
    public int ParameterId { get; set; }

    public bool IsRequired { get; set; } = true;

    public decimal? MinValue { get; set; }

    public decimal? MaxValue { get; set; }
}
