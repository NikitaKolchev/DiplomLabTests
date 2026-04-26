namespace Bmz.LabTests.Application.Auth;

public sealed class LoginResponse
{
    public required string Token { get; init; }

    public required DateTime ExpiresAtUtc { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }
}
