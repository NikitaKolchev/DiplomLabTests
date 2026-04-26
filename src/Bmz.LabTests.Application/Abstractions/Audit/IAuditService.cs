namespace Bmz.LabTests.Application.Abstractions.Audit;

public interface IAuditService
{
    Task WriteAsync(int? actorUserId, string? actorLogin, string actionType, string entityType, string? entityId, string? details, CancellationToken cancellationToken);
}
