using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

public sealed class OrganizationRepository(ApplicationDbContext dbContext) : IOrganizationRepository
{
    public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken)
        => dbContext.Roles.FirstOrDefaultAsync(x => x.Name == roleName, cancellationToken);

    public Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken)
        => dbContext.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken)
        => dbContext.Users.Include(x => x.Role).FirstOrDefaultAsync(x => x.Login == login, cancellationToken);

    public Task<User?> GetEngineerByIdAsync(int userId, CancellationToken cancellationToken)
        => dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.Role.Name == Roles.Engineer, cancellationToken);

    public Task<User?> GetAssistantByIdAsync(int userId, CancellationToken cancellationToken)
        => dbContext.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId && x.Role.Name == Roles.Assistant, cancellationToken);

    public Task<List<User>> GetEngineersAsync(CancellationToken cancellationToken)
        => dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.Role.Name == Roles.Engineer)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public async Task<List<User>> GetAssistantsForEngineerAsync(int engineerUserId, string? search, string? login, CancellationToken cancellationToken)
    {
        var engineer = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.Id == engineerUserId)
            .Select(x => new { x.LaboratoryId })
            .FirstOrDefaultAsync(cancellationToken);
        if (engineer?.LaboratoryId is null)
            return [];

        var query = dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Where(x => x.Role.Name == Roles.Assistant && x.LaboratoryId == engineer.LaboratoryId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim();
            query = query.Where(x => x.FullName.Contains(normalized));
        }

        if (!string.IsNullOrWhiteSpace(login))
        {
            var normalized = login.Trim();
            query = query.Where(x => x.Login.Contains(normalized));
        }

        return await query.OrderBy(x => x.FullName).ToListAsync(cancellationToken);
    }

    public Task<List<Laboratory>> GetLaboratoriesAsync(CancellationToken cancellationToken)
        => dbContext.Laboratories
            .AsNoTracking()
            .Include(x => x.Users)
                .ThenInclude(u => u.Role)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Laboratory?> GetLaboratoryByIdAsync(int laboratoryId, CancellationToken cancellationToken)
        => dbContext.Laboratories
            .Include(x => x.Users)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(x => x.Id == laboratoryId, cancellationToken);

    public Task AddUserAsync(User user, CancellationToken cancellationToken)
        => dbContext.Users.AddAsync(user, cancellationToken).AsTask();

    public Task AddRoleAsync(Role role, CancellationToken cancellationToken)
        => dbContext.Roles.AddAsync(role, cancellationToken).AsTask();

    public Task AddLaboratoryAsync(Laboratory laboratory, CancellationToken cancellationToken)
        => dbContext.Laboratories.AddAsync(laboratory, cancellationToken).AsTask();

    public Task<bool> IsEngineerAssignedToAnotherLaboratoryAsync(int engineerId, int? excludingLaboratoryId, CancellationToken cancellationToken)
        => dbContext.Users
            .AnyAsync(x => x.Id == engineerId 
                        && x.Role.Name == Roles.Engineer
                        && x.LaboratoryId.HasValue 
                        && (!excludingLaboratoryId.HasValue || x.LaboratoryId != excludingLaboratoryId.Value), 
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);

    public Task<List<User>> GetAssistantsAsync(CancellationToken cancellationToken)
        => dbContext.Users
            .AsNoTracking()
            .Include(x => x.Role)
            .Include(x => x.Laboratory)
            .Where(x => x.Role.Name == Roles.Assistant)
            .OrderBy(x => x.FullName)
            .ToListAsync(cancellationToken);

    public void RemoveUser(User user) => dbContext.Users.Remove(user);

    public void RemoveLaboratory(Laboratory laboratory) => dbContext.Laboratories.Remove(laboratory);

    public async Task ClearLaboratoryFromUsersAsync(int laboratoryId, CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.Where(x => x.LaboratoryId == laboratoryId).ToListAsync(cancellationToken);
        foreach (var u in users)
            u.LaboratoryId = null;
    }
}
