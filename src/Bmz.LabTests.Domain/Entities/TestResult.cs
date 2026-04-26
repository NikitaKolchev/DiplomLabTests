using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Domain.Entities;

public sealed class TestResult : BaseEntity
{
    private readonly List<TestValue> _values = new();

    // EF requirement
    private TestResult() { }

    public TestResult(
        int assistantId,
        int wireCodeId,
        int laboratoryId,
        string batchNumber,
        int? customerId)
        : this(DateTime.UtcNow, DateTime.UtcNow, assistantId, wireCodeId, laboratoryId, batchNumber, customerId, TestResultStatus.InProgress)
    {
    }

    public TestResult(
        DateTime date,
        DateTime updatedAtUtc,
        int assistantId,
        int wireCodeId,
        int laboratoryId,
        string batchNumber,
        int? customerId,
        TestResultStatus status)
    {
        Date = date;
        UpdatedAtUtc = updatedAtUtc;
        AssistantId = assistantId;
        WireCodeId = wireCodeId;
        LaboratoryId = laboratoryId;
        BatchNumber = batchNumber.Trim();
        CustomerId = customerId;
        Status = status;
    }

    public DateTime Date { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public int AssistantId { get; private set; }

    public User Assistant { get; private set; } = null!;

    public int WireCodeId { get; private set; }

    public WireCode WireCode { get; private set; } = null!;

    public int LaboratoryId { get; private set; }

    public Laboratory Laboratory { get; private set; } = null!;

    public string BatchNumber { get; private set; } = string.Empty;

    public int? CustomerId { get; private set; }

    public Customer? Customer { get; private set; }

    public TestResultStatus Status { get; private set; } = TestResultStatus.InProgress;

    public ICollection<TestValue> Values => _values;

    public FinalProduct? FinalProduct { get; private set; }

    public Reject? Reject { get; private set; }

    public void AddOrUpdateValue(int parameterId, string value)
    {
        if (Status == TestResultStatus.Completed)
            throw new InvalidOperationException("Нельзя изменять завершенные испытания.");

        var existing = _values.FirstOrDefault(x => x.ParameterId == parameterId);
        var normalizedValue = value.Trim();

        if (existing != null)
        {
            existing.Update(normalizedValue);
        }
        else
        {
            _values.Add(new TestValue(parameterId, normalizedValue));
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Complete()
    {
        if (Status == TestResultStatus.Completed)
            return;

        Status = TestResultStatus.Completed;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
