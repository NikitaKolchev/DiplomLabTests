namespace Bmz.LabTests.Application.Abstractions.Auth;

public interface IPasswordVerifier
{
    bool Verify(string password, string hash);
}
