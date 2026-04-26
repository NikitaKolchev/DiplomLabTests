using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;

namespace Bmz.LabTests.Domain.Entities;

public sealed class Laboratory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();

    /// <summary>
    /// Возвращает инженера лаборатории (пользователя с ролью Engineer).
    /// </summary>
    public User? GetEngineer() => Users.FirstOrDefault(u => u.Role.Name == Roles.Engineer);
}
