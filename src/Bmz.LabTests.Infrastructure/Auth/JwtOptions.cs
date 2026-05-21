namespace Bmz.LabTests.Infrastructure.Auth;

/// <summary>
/// Параметры конфигурации для генерации и валидации JWT токенов.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int ExpireMinutes { get; set; } = 60;
}
