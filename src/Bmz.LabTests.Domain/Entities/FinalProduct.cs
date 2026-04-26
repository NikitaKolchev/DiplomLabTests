using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

public sealed class FinalProduct : BaseEntity
{
    private FinalProduct() { }

    public FinalProduct(int testResultId) : this(testResultId, DateTime.UtcNow) { }

    public FinalProduct(int testResultId, DateTime closedAtUtc)
    {
        TestResultId = testResultId;
        ClosedAtUtc = closedAtUtc;
    }

    public int TestResultId { get; private set; }

    public TestResult TestResult { get; private set; } = null!;

    public DateTime ClosedAtUtc { get; private set; }
}
