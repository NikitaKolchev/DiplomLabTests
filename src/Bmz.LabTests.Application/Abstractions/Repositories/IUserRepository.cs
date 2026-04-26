using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken);
    Task<User?> FindByIdWithLaboratoryAsync(int userId, CancellationToken cancellationToken);
}
