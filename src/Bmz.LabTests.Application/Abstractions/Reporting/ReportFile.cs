namespace Bmz.LabTests.Application.Abstractions.Reporting;

public sealed class ReportFile
{
    public required byte[] Content { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }
}
