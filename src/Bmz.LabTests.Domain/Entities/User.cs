using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Domain.Entities;

/// <summary>
/// Сущность пользователя системы.
/// Поддерживает как локальные учетные записи, так и доменную авторизацию (через Sid).
/// </summary>
public sealed class User : BaseEntity
{
    public string Sid { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public bool IsLocalAccount { get; set; }

    public string? PasswordHash { get; set; }

    public int RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public int? LaboratoryId { get; set; }

    public Laboratory? Laboratory { get; set; }

    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
