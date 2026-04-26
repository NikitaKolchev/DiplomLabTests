using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Persistence.Seeding;

public sealed class AdminSeeder(IPasswordHasher passwordHasher) : IEntitySeeder
{
    private const string LocalAdminLogin = "local-admin";
    private const string LocalAdminPassword = "VeryHardPassword";

    public int Order => 2;

    public async Task<Result> SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        try
        {
            var localAdmin = await context.Users
                .FirstOrDefaultAsync(x => x.IsLocalAccount && x.Login == LocalAdminLogin, cancellationToken);

            if (localAdmin is not null)
                return Result.Success();

            var adminRoleId = await context.Roles
                .Where(x => x.Name == Roles.Admin)
                .Select(x => x.Id)
                .SingleAsync(cancellationToken);

            context.Users.Add(new User
            {
                Login = LocalAdminLogin,
                FullName = "Local Admin",
                Sid = "LOCAL-ADMIN",
                IsLocalAccount = true,
                PasswordHash = passwordHasher.Hash(LocalAdminPassword),
                RoleId = adminRoleId
            });
            await context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при создании администратора: {ex.Message}");
        }
    }
}