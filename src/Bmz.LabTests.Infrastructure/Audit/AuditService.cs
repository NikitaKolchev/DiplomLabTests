using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;

namespace Bmz.LabTests.Infrastructure.Audit;

/// <summary>
/// Сервис логирования действий пользователей (аудит).
/// Позволяет отслеживать изменения в системе для последующего анализа и разбора инцидентов.
/// </summary>
public sealed class AuditService(ApplicationDbContext dbContext) : IAuditService
{
    /// <summary>
    /// Добавляет запись в лог аудита (синхронно, без сохранения в БД).
    /// </summary>
    public void Write(
        int? actorUserId,
        string? actorLogin,
        string actionType,
        string entityType,
        string? entityId,
        string? details)
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
    }

    /// <summary>
    /// Добавляет запись в лог аудита и сохраняет изменения в базе данных.
    /// </summary>
    public Task WriteAsync(
        int? actorUserId,
        string? actorLogin,
        string actionType,
        string entityType,
        string? entityId,
        string? details,
        CancellationToken cancellationToken)
    {
        Write(actorUserId, actorLogin, actionType, entityType, entityId, details);
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
