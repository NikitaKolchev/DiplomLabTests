using Bmz.LabTests.Application.Abstractions.Audit;
using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Organization;

public sealed class UserManagementService(
    IOrganizationRepository repository,
    IPasswordHasher passwordHasher,
    IAuditService auditService) : IUserManagementService
{
    public string TransliterateLogin(string fullName) => UserUtils.Transliterate(fullName);

    public string GeneratePassword(int length = 10) => UserUtils.GeneratePassword(length);

    public async Task<Result<UserSummaryDto>> UpdateUserRoleAsync(int actorUserId, string? actorLogin, int userId, string newRoleName, CancellationToken cancellationToken)
    {
        try
        {
            var user = await repository.GetUserByIdAsync(userId, cancellationToken);
            if (user is null)
                return Result.Failure<UserSummaryDto>("Пользователь не найден.");

            var currentRole = await repository.GetRoleByNameAsync(newRoleName, cancellationToken);
            if (currentRole is null)
                return Result.Failure<UserSummaryDto>($"Роль '{newRoleName}' не найдена.");

            var oldRoleName = user.Role?.Name ?? "Unknown";
            user.RoleId = currentRole.Id;

            if (oldRoleName != newRoleName)
            {
                user.LaboratoryId = null;
            }

            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Update", "UserRole", userId.ToString(), $"Changed user '{user.Login}' role from {oldRoleName} to {newRoleName}", cancellationToken);

            return Result.Success(new UserSummaryDto(user.Id, user.FullName, user.Login, newRoleName, user.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при обновлении роли пользователя: {ex.Message}");
        }
    }

    public async Task<Result<UserSummaryDto>> CreateEngineerAsync(int actorUserId, string? actorLogin, string fullName, string login, string password, int? laboratoryId, CancellationToken cancellationToken)
    {
        try
        {
            if (await repository.GetUserByLoginAsync(login.Trim(), cancellationToken) is not null)
            {
                return Result.Failure<UserSummaryDto>("Логин уже используется.");
            }

            var engineerRole = await repository.GetRoleByNameAsync(Roles.Engineer, cancellationToken);
            if (engineerRole is null)
            {
                engineerRole = new Role { Name = Roles.Engineer };
                await repository.AddRoleAsync(engineerRole, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
            }

            Laboratory? laboratory = null;
            if (laboratoryId.HasValue)
            {
                laboratory = await repository.GetLaboratoryByIdAsync(laboratoryId.Value, cancellationToken);
                if (laboratory is null)
                    return Result.Failure<UserSummaryDto>("Лаборатория не найдена.");
            }

            var engineer = new User
            {
                FullName = fullName.Trim(),
                Login = login.Trim(),
                PasswordHash = passwordHasher.Hash(password),
                IsLocalAccount = true,
                Sid = $"LOCAL-{Guid.NewGuid():N}",
                RoleId = engineerRole.Id,
                LaboratoryId = laboratory?.Id
            };

            await repository.AddUserAsync(engineer, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(actorUserId, actorLogin, "Create", "User", engineer.Id.ToString(), $"Created Engineer '{engineer.Login}'", cancellationToken);

            return Result.Success(new UserSummaryDto(engineer.Id, engineer.FullName, engineer.Login, Roles.Engineer, engineer.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при создании инженера: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetEngineersAsync(CancellationToken cancellationToken)
    {
        var engineers = await repository.GetEngineersAsync(cancellationToken);
        var dtos = engineers.Select(x => new UserSummaryDto(x.Id, x.FullName, x.Login, Roles.Engineer, x.LaboratoryId)).ToArray();
        return Result.Success<IReadOnlyCollection<UserSummaryDto>>(dtos);
    }

    public async Task<Result<UserSummaryDto>> UpdateEngineerAsync(int actorUserId, string? actorLogin, int engineerId, string fullName, string login, string? password, int? laboratoryId, CancellationToken cancellationToken)
    {
        try
        {
            var engineer = await repository.GetEngineerByIdAsync(engineerId, cancellationToken);
            if (engineer is null)
                return Result.Failure<UserSummaryDto>("Инженер не найден.");

            var normalizedLogin = login.Trim();
            var duplicate = await repository.GetUserByLoginAsync(normalizedLogin, cancellationToken);
            if (duplicate is not null && duplicate.Id != engineerId)
                return Result.Failure<UserSummaryDto>("Логин уже используется.");

            engineer.FullName = fullName.Trim();
            engineer.Login = normalizedLogin;
            if (!string.IsNullOrWhiteSpace(password))
            {
                engineer.PasswordHash = passwordHasher.Hash(password);
            }

            if (laboratoryId.HasValue)
            {
                var laboratory = await repository.GetLaboratoryByIdAsync(laboratoryId.Value, cancellationToken);
                if (laboratory is null)
                    return Result.Failure<UserSummaryDto>("Лаборатория не найдена.");

                if (await repository.IsEngineerAssignedToAnotherLaboratoryAsync(engineerId, laboratoryId.Value, cancellationToken))
                    return Result.Failure<UserSummaryDto>("Инженер уже назначен в другую лабораторию.");

                engineer.LaboratoryId = laboratory.Id;
            }
            else
            {
                engineer.LaboratoryId = null;
            }

            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Update", "User", engineerId.ToString(), $"Updated Engineer '{engineer.Login}'", cancellationToken);
            return Result.Success(new UserSummaryDto(engineer.Id, engineer.FullName, engineer.Login, Roles.Engineer, engineer.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при обновлении инженера: {ex.Message}");
        }
    }

    public async Task<Result> DeleteEngineerAsync(int actorUserId, string? actorLogin, int engineerId, CancellationToken cancellationToken)
    {
        try
        {
            var engineer = await repository.GetEngineerByIdAsync(engineerId, cancellationToken);
            if (engineer is null)
                return Result.Failure("Инженер не найден.");

            engineer.LaboratoryId = null;

            repository.RemoveUser(engineer);
            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Delete", "User", engineerId.ToString(), $"Deleted Engineer '{engineer.Login}'", cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении инженера: {ex.Message}");
        }
    }

    public async Task<Result<UserSummaryDto>> CreateAssistantByAdminAsync(int actorUserId, string? actorLogin, string fullName, string login, string password, int laboratoryId, CancellationToken cancellationToken)
    {
        try
        {
            if (await repository.GetUserByLoginAsync(login.Trim(), cancellationToken) is not null)
            {
                return Result.Failure<UserSummaryDto>("Логин уже используется.");
            }

            var laboratory = await repository.GetLaboratoryByIdAsync(laboratoryId, cancellationToken);
            if (laboratory is null)
                return Result.Failure<UserSummaryDto>("Лаборатория не найдена.");

            var assistantRole = await repository.GetRoleByNameAsync(Roles.Assistant, cancellationToken);
            if (assistantRole is null)
            {
                assistantRole = new Role { Name = Roles.Assistant };
                await repository.AddRoleAsync(assistantRole, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
            }

            var assistant = new User
            {
                FullName = fullName.Trim(),
                Login = login.Trim(),
                PasswordHash = passwordHasher.Hash(password),
                IsLocalAccount = true,
                Sid = $"LOCAL-{Guid.NewGuid():N}",
                RoleId = assistantRole.Id,
                LaboratoryId = laboratory.Id
            };

            await repository.AddUserAsync(assistant, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(actorUserId, actorLogin, "Create", "User", assistant.Id.ToString(), $"Created Assistant '{assistant.Login}'", cancellationToken);

            return Result.Success(new UserSummaryDto(assistant.Id, assistant.FullName, assistant.Login, Roles.Assistant, assistant.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при создании лаборанта: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetAssistantsForAdminAsync(CancellationToken cancellationToken)
    {
        var assistants = await repository.GetAssistantsAsync(cancellationToken);
        var dtos = assistants.Select(x => new UserSummaryDto(x.Id, x.FullName, x.Login, Roles.Assistant, x.LaboratoryId)).ToArray();
        return Result.Success<IReadOnlyCollection<UserSummaryDto>>(dtos);
    }

    public async Task<Result<UserSummaryDto>> UpdateAssistantByAdminAsync(int actorUserId, string? actorLogin, int assistantId, string fullName, string login, string? password, int laboratoryId, CancellationToken cancellationToken)
    {
        try
        {
            var assistant = await repository.GetAssistantByIdAsync(assistantId, cancellationToken);
            if (assistant is null)
                return Result.Failure<UserSummaryDto>("Лаборант не найден.");

            var laboratory = await repository.GetLaboratoryByIdAsync(laboratoryId, cancellationToken);
            if (laboratory is null)
                return Result.Failure<UserSummaryDto>("Лаборатория не найдена.");

            var normalizedLogin = login.Trim();
            var duplicate = await repository.GetUserByLoginAsync(normalizedLogin, cancellationToken);
            if (duplicate is not null && duplicate.Id != assistantId)
                return Result.Failure<UserSummaryDto>("Логин уже используется.");

            assistant.FullName = fullName.Trim();
            assistant.Login = normalizedLogin;
            assistant.LaboratoryId = laboratoryId;
            if (!string.IsNullOrWhiteSpace(password))
            {
                assistant.PasswordHash = passwordHasher.Hash(password);
            }

            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Update", "User", assistantId.ToString(), $"Admin updated Assistant '{assistant.Login}'", cancellationToken);
            return Result.Success(new UserSummaryDto(assistant.Id, assistant.FullName, assistant.Login, Roles.Assistant, assistant.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при обновлении лаборанта администратором: {ex.Message}");
        }
    }

    public async Task<Result> DeleteAssistantAsync(int actorUserId, string? actorLogin, int assistantId, CancellationToken cancellationToken)
    {
        try
        {
            var assistant = await repository.GetAssistantByIdAsync(assistantId, cancellationToken);
            if (assistant is null)
                return Result.Failure("Лаборант не найден.");

            repository.RemoveUser(assistant);
            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(actorUserId, actorLogin, "Delete", "User", assistantId.ToString(), $"Deleted Assistant '{assistant.Login}'", cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Ошибка при удалении лаборанта: {ex.Message}");
        }
    }

    public async Task<Result<UserSummaryDto>> CreateAssistantByEngineerAsync(int engineerUserId, string? actorLogin, string fullName, string login, string password, CancellationToken cancellationToken)
    {
        try
        {
            if (await repository.GetUserByLoginAsync(login.Trim(), cancellationToken) is not null)
            {
                return Result.Failure<UserSummaryDto>("Логин уже используется.");
            }

            var engineer = await repository.GetEngineerByIdAsync(engineerUserId, cancellationToken);
            if (engineer is null)
                return Result.Failure<UserSummaryDto>("Профиль инженера не найден.");

            if (!engineer.LaboratoryId.HasValue)
            {
                return Result.Failure<UserSummaryDto>("Инженер не назначен в лабораторию.");
            }

            var assistantRole = await repository.GetRoleByNameAsync(Roles.Assistant, cancellationToken);
            if (assistantRole is null)
                return Result.Failure<UserSummaryDto>("Роль Лаборант не найдена.");

            var assistant = new User
            {
                FullName = fullName.Trim(),
                Login = login.Trim(),
                PasswordHash = passwordHasher.Hash(password),
                IsLocalAccount = true,
                Sid = $"LOCAL-{Guid.NewGuid():N}",
                RoleId = assistantRole.Id,
                LaboratoryId = engineer.LaboratoryId
            };

            await repository.AddUserAsync(assistant, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);

            await auditService.WriteAsync(engineerUserId, actorLogin, "Create", "User", assistant.Id.ToString(), $"Engineer created Assistant '{assistant.Login}'", cancellationToken);

            return Result.Success(new UserSummaryDto(assistant.Id, assistant.FullName, assistant.Login, Roles.Assistant, assistant.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при создании лаборанта инженером: {ex.Message}");
        }
    }

    public async Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetAssistantsForEngineerAsync(int engineerUserId, string? search, string? login, CancellationToken cancellationToken)
    {
        var assistants = await repository.GetAssistantsForEngineerAsync(engineerUserId, search, login, cancellationToken);
        var dtos = assistants.Select(x => new UserSummaryDto(x.Id, x.FullName, x.Login, Roles.Assistant, x.LaboratoryId)).ToArray();
        return Result.Success<IReadOnlyCollection<UserSummaryDto>>(dtos);
    }

    public async Task<Result<UserSummaryDto>> UpdateAssistantByEngineerAsync(int engineerUserId, int assistantId, string fullName, string login, string? password, CancellationToken cancellationToken)
    {
        try
        {
            var assistant = await repository.GetAssistantByIdAsync(assistantId, cancellationToken);
            if (assistant is null)
                return Result.Failure<UserSummaryDto>("Лаборант не найден.");

            var engineer = await repository.GetEngineerByIdAsync(engineerUserId, cancellationToken);
            if (engineer?.LaboratoryId is null || assistant.LaboratoryId != engineer.LaboratoryId)
                return Result.Failure<UserSummaryDto>("Нет прав на редактирование этого лаборанта.");

            var normalizedLogin = login.Trim();
            var duplicate = await repository.GetUserByLoginAsync(normalizedLogin, cancellationToken);
            if (duplicate is not null && duplicate.Id != assistantId)
                return Result.Failure<UserSummaryDto>("Логин уже используется.");

            assistant.FullName = fullName.Trim();
            assistant.Login = normalizedLogin;
            if (!string.IsNullOrWhiteSpace(password))
            {
                assistant.PasswordHash = passwordHasher.Hash(password);
            }

            await repository.SaveChangesAsync(cancellationToken);
            await auditService.WriteAsync(engineerUserId, null, "Update", "User", assistant.Id.ToString(), $"Engineer updated Assistant '{assistant.Login}'", cancellationToken);

            return Result.Success(new UserSummaryDto(assistant.Id, assistant.FullName, assistant.Login, Roles.Assistant, assistant.LaboratoryId));
        }
        catch (Exception ex)
        {
            return Result.Failure<UserSummaryDto>($"Ошибка при обновлении лаборанта инженером: {ex.Message}");
        }
    }
}
