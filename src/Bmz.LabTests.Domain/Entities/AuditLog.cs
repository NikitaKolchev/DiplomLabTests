using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность лога аудита.
/// Предназначена для отслеживания всех критических изменений в системе (кто, когда и что изменил).
/// </summary>
public sealed class AuditLog : BaseEntity
{
    public DateTime TimestampUtc { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorLogin { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    public string? Details { get; set; }
}
