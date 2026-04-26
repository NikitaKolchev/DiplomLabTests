namespace Bmz.LabTests.API.Contracts.TestResults;

public sealed class SaveTestValuesRequest
{
    public string RowVersion { get; set; } = string.Empty;

    public List<TestValueItemRequest> Values { get; set; } = [];
}

public sealed class TestValueItemRequest
{
    public int ParameterId { get; set; }

    public string Value { get; set; } = string.Empty;
}
