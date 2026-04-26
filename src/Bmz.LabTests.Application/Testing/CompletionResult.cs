namespace Bmz.LabTests.Application.Testing;

public sealed class CompletionResult
{
    public bool IsAccepted { get; init; }

    public string? RejectReason { get; init; }
}
