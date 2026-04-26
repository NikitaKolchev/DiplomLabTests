using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Auth;

public interface ILdapService
{
    Task<Result<bool>> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken);

    Task<Result<LdapUserInfo>> GetUserInfoAsync(string username, CancellationToken cancellationToken);
}

public record LdapUserInfo(string Login, string FullName, string? Email);
