using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность роли пользователя.
/// Определяет уровень доступа в системе (Admin, Engineer, Assistant, Guest).
/// </summary>
public sealed class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}
