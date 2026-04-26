using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;

namespace Bmz.LabTests.Infrastructure.Audit;

public sealed class AuditService(ApplicationDbContext dbContext) : IAuditService
{
    public async Task WriteAsync(
        int? actorUserId,
        string? actorLogin,
        string actionType,
        string entityType,
        string? entityId,
        string? details,
        CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            TimestampUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            ActorLogin = actorLogin,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
