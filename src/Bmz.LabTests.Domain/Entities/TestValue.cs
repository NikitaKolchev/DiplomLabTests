using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

public sealed class TestValue : BaseEntity
{
    // EF requirement
    private TestValue() { }

    public TestValue(int parameterId, string value)
    {
        ParameterId = parameterId;
        Value = value;
    }

    public TestValue(int testResultId, int parameterId, string value)
    {
        TestResultId = testResultId;
        ParameterId = parameterId;
        Value = value;
    }

    public int TestResultId { get; private set; }

    public TestResult TestResult { get; private set; } = null!;

    public int ParameterId { get; private set; }

    public Parameter Parameter { get; private set; } = null!;

    public string Value { get; private set; } = string.Empty;

    internal void Update(string newValue)
    {
        Value = newValue;
    }
}
