using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

public sealed class UserRepository(ApplicationDbContext context) : IUserRepository
{
    public Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
    }

    public Task<User?> FindByIdWithLaboratoryAsync(int userId, CancellationToken cancellationToken)
    {
        return context.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Laboratory)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken)
    {
        return context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<int?> GetLaboratoryIdByEngineerIdAsync(int engineerUserId, CancellationToken cancellationToken)
    {
        return context.Users
            .Where(x => x.Id == engineerUserId && x.Role.Name == Roles.Engineer)
            .Select(x => (int?)x.LaboratoryId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
