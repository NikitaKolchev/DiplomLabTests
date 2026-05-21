using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность "Брак".
/// Создается автоматически при завершении протокола, если хотя бы один обязательный параметр не соответствует нормам.
/// </summary>
public sealed class Reject : BaseEntity
{
    private Reject() { }

    public Reject(int testResultId, string reason) : this(testResultId, reason, DateTime.UtcNow) { }

    public Reject(int testResultId, string reason, DateTime closedAtUtc)
    {
        TestResultId = testResultId;
        Reason = reason;
        ClosedAtUtc = closedAtUtc;
    }

    public int TestResultId { get; private set; }

    public TestResult TestResult { get; private set; } = null!;

    public string Reason { get; private set; } = string.Empty;

    public DateTime ClosedAtUtc { get; private set; }
}
