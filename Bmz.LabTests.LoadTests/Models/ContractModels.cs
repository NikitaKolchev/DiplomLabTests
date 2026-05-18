namespace Bmz.LabTests.LoadTests.Models;

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class CreateTestResultRequest
{
    public int WireCodeId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
}

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

public sealed class CompleteTestResultRequest
{
    public string RowVersion { get; set; } = string.Empty;
}
