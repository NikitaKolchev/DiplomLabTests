using Bmz.LabTests.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace Bmz.LabTests.Infrastructure.Auth;

/// <summary>
/// Сервис для безопасного хеширования паролей.
/// Использует алгоритм PBKDF2 с солью для защиты от атак по словарю и радужных таблиц.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    /// <summary>
    /// Создает криптографически стойкий хеш пароля.
    /// Результат возвращается в формате "соль.хеш" (Base64).
    /// </summary>
    public string Hash(string password)
    {
        Span<byte> salt = stackalloc byte[16];
        RandomNumberGenerator.Fill(salt);

        var subkey = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt.ToArray(),
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100_000,
            numBytesRequested: 32);

        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(subkey)}";
    }
}
