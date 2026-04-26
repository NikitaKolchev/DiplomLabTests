using Bmz.LabTests.Application.Auth;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Auth;

public interface IIdentityProvider
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
