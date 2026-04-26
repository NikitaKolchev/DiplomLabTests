using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Persistence.Seeding;

public sealed class RoleSeeder : IEntitySeeder
{
    public int Order => 1;

    public async Task<Result> SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var requiredRoles = new[] { Roles.Admin, Roles.Engineer, Roles.Assistant, Roles.Guest };
            var existingRoles = await context.Roles
                .Where(r => requiredRoles.Contains(r.Name))
                .Select(r => r.Name)
                .ToListAsync(cancellationToken);

            var rolesToAdd = requiredRoles
                .Where(name => !existingRoles.Contains(name))
                .Select(name => new Role { Name = name })
                .ToList();

            if (rolesToAdd.Count != 0)
            {
                context.Roles.AddRange(rolesToAdd);
                await context.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при наполнении ролей: {ex.Message}");
        }
    }
}