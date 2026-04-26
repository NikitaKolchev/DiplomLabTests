using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken);
    Task<User?> FindByIdWithLaboratoryAsync(int userId, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(int userId, CancellationToken cancellationToken);
    Task<int?> GetLaboratoryIdByEngineerIdAsync(int engineerUserId, CancellationToken cancellationToken);
}
