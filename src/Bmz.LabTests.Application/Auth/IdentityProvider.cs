using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Auth;

public sealed class IdentityProvider(
    IUserRepository userRepository,
    ILdapService ldapService,
    ITokenService tokenService,
    IPasswordVerifier passwordVerifier) : IIdentityProvider
{
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await userRepository.FindByLoginAsync(request.Username, cancellationToken);
            if (user is null)
            {
                return Result.Failure<LoginResponse>("Неверные учетные данные.");
            }

            bool isValid;
            if (user.IsLocalAccount)
            {
                isValid = !string.IsNullOrWhiteSpace(user.PasswordHash) && passwordVerifier.Verify(request.Password, user.PasswordHash);
            }
            else
            {
                var ldapResult = await ldapService.ValidateCredentialsAsync(request.Username, request.Password, cancellationToken);
                if (ldapResult.IsFailure)
                {
                    return Result.Failure<LoginResponse>(ldapResult.Error ?? "Ошибка LDAP без описания.");
                }
                isValid = ldapResult.Value;
            }

            if (!isValid)
            {
                return Result.Failure<LoginResponse>("Неверные учетные данные.");
            }

            var (token, expiresAtUtc) = tokenService.GenerateToken(user);

            return Result.Success(new LoginResponse
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                FullName = user.FullName,
                Role = user.Role.Name
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<LoginResponse>($"Ошибка при входе: {ex.Message}");
        }
    }
}
