using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Auth;

/// <summary>
/// Интерфейс сервиса для работы с JWT токенами.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Генерирует JWT токен для указанного пользователя.
    /// </summary>
    /// <param name="user">Сущность пользователя, для которого создается токен.</param>
    /// <returns>Кортеж, содержащий строку токена и дату его истечения в UTC.</returns>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
