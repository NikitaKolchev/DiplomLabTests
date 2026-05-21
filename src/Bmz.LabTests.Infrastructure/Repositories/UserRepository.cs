using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с данными пользователей.
/// Инкапсулирует запросы к таблице Users с использованием Entity Framework.
/// </summary>
public sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    /// <summary>
    /// Ищет пользователя по логину, включая информацию о его роли.
    /// </summary>
    public Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
    }

    /// <summary>
    /// Получает профиль пользователя со связанной лабораторией без отслеживания изменений (AsNoTracking).
    /// </summary>
    public Task<User?> FindByIdWithLaboratoryAsync(int userId, CancellationToken cancellationToken)
    {
        return context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Laboratory)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Получает пользователя по ID с загрузкой роли.
    /// </summary>
    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    /// <summary>
    /// Возвращает идентификатор лаборатории, за которой закреплен инженер.
    /// </summary>
    public Task<int?> GetLaboratoryIdByEngineerIdAsync(int engineerUserId, CancellationToken cancellationToken)
    {
        return context.Users
            .Where(x => x.Id == engineerUserId && x.Role.Name == Roles.Engineer)
            .Select(x => (int?)x.LaboratoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
