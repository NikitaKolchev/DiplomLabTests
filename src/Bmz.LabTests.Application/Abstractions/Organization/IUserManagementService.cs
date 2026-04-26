using Bmz.LabTests.Application.Organization;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Organization;

public interface IUserManagementService
{
    // Users (Common)
    Task<Result<UserSummaryDto>> UpdateUserRoleAsync(int actorUserId, string? actorLogin, int userId, string newRoleName, CancellationToken cancellationToken);

    // Engineers
    Task<Result<UserSummaryDto>> CreateEngineerAsync(int actorUserId, string? actorLogin, string fullName, string login, string password, int? laboratoryId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetEngineersAsync(CancellationToken cancellationToken);
    Task<Result<UserSummaryDto>> UpdateEngineerAsync(int actorUserId, string? actorLogin, int engineerId, string fullName, string login, string? password, int? laboratoryId, CancellationToken cancellationToken);
    Task<Result> DeleteEngineerAsync(int actorUserId, string? actorLogin, int engineerId, CancellationToken cancellationToken);

    // Assistants (Admin)
    Task<Result<UserSummaryDto>> CreateAssistantByAdminAsync(int actorUserId, string? actorLogin, string fullName, string login, string password, int laboratoryId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetAssistantsForAdminAsync(CancellationToken cancellationToken);
    Task<Result<UserSummaryDto>> UpdateAssistantByAdminAsync(int actorUserId, string? actorLogin, int assistantId, string fullName, string login, string? password, int laboratoryId, CancellationToken cancellationToken);
    Task<Result> DeleteAssistantAsync(int actorUserId, string? actorLogin, int assistantId, CancellationToken cancellationToken);

    // Assistants (Engineer)
    Task<Result<UserSummaryDto>> CreateAssistantByEngineerAsync(int engineerUserId, string? actorLogin, string fullName, string login, string password, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<UserSummaryDto>>> GetAssistantsForEngineerAsync(int engineerUserId, string? search, string? login, CancellationToken cancellationToken);
    Task<Result<UserSummaryDto>> UpdateAssistantByEngineerAsync(int engineerUserId, int assistantId, string fullName, string login, string? password, CancellationToken cancellationToken);

    // Utility endpoints
    string TransliterateLogin(string fullName);
    string GeneratePassword(int length = 10);
}
