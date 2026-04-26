using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface IOrganizationRepository
{
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken);
    Task<User?> GetUserByLoginAsync(string login, CancellationToken cancellationToken);
    Task<User?> GetEngineerByIdAsync(int userId, CancellationToken cancellationToken);
    Task<User?> GetAssistantByIdAsync(int userId, CancellationToken cancellationToken);
    Task<List<User>> GetEngineersAsync(CancellationToken cancellationToken);
    Task<List<User>> GetAssistantsForEngineerAsync(int engineerUserId, string? search, string? login, CancellationToken cancellationToken);
    Task<List<Laboratory>> GetLaboratoriesAsync(CancellationToken cancellationToken);
    Task<Laboratory?> GetLaboratoryByIdAsync(int laboratoryId, CancellationToken cancellationToken);
    Task AddUserAsync(User user, CancellationToken cancellationToken);
    Task AddRoleAsync(Role role, CancellationToken cancellationToken);
    Task AddLaboratoryAsync(Laboratory laboratory, CancellationToken cancellationToken);
    Task<bool> IsEngineerAssignedToAnotherLaboratoryAsync(int engineerId, int? excludingLaboratoryId, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<List<User>> GetAssistantsAsync(CancellationToken cancellationToken);
    void RemoveUser(User user);
    void RemoveLaboratory(Laboratory laboratory);
    Task ClearLaboratoryFromUsersAsync(int laboratoryId, CancellationToken cancellationToken);
}
