namespace Bmz.LabTests.Application.Abstractions.Products;

public sealed record ProductListItemDto(
    int Id,
    DateTime Date,
    string BatchNumber,
    string WireCode,
    string Laboratory,
    string? CustomerName,
    string Assistant,
    bool IsAccepted,
    string? RejectReason);
