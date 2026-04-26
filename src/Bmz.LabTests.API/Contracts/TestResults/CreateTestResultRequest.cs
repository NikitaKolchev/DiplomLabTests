namespace Bmz.LabTests.API.Contracts.TestResults;

public sealed class CreateTestResultRequest
{
    public int WireCodeId { get; set; }

    public string BatchNumber { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
}
