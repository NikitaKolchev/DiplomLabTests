using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
