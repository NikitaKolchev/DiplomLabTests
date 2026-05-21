using Bmz.LabTests.Application.Auth;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Auth;

/// <summary>
/// Интерфейс провайдера идентификации.
/// Отвечает за проверку учетных данных пользователя (через Active Directory или локальную БД).
/// </summary>
public interface IIdentityProvider
{
    /// <summary>
    /// Выполняет аутентификацию пользователя.
    /// </summary>
    /// <param name="request">Данные для входа (логин/пароль).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Результат с JWT токеном и информацией о пользователе в случае успеха.</returns>
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
