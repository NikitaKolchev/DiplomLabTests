using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Auth;

/// <summary>
/// Реализация провайдера идентификации.
/// Оркестрирует процесс входа, проверяя тип аккаунта (локальный/доменный) и вызывая соответствующие сервисы проверки.
/// </summary>
public sealed class IdentityProvider(
    IUserRepository userRepository,
    ILdapService ldapService,
    ITokenService tokenService,
    IPasswordVerifier passwordVerifier) : IIdentityProvider
{
    /// <summary>
    /// Выполняет вход в систему.
    /// </summary>
    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // 1. Поиск пользователя в локальной базе данных по логину
            var user = await userRepository.FindByLoginAsync(request.Username, cancellationToken);
            if (user is null)
            {
                return Result.Failure<LoginResponse>("Неверные учетные данные.");
            }

            bool isValid;
            if (user.IsLocalAccount)
            {
                // 2а. Проверка пароля для локальной учетной записи
                isValid = !string.IsNullOrWhiteSpace(user.PasswordHash) && passwordVerifier.Verify(request.Password, user.PasswordHash);
            }
            else
            {
                // 2б. Проверка учетных данных через Active Directory (LDAP) для доменных пользователей
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

            // 3. Генерация JWT токена при успешной проверке
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
            // Логирование исключения и возврат результата с ошибкой
            return Result.Failure<LoginResponse>($"Ошибка при входе: {ex.Message}");
        }
    }
}
