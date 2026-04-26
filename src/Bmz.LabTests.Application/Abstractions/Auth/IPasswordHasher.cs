namespace Bmz.LabTests.Application.Abstractions.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
}
