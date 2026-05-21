using Bmz.LabTests.Application.Abstractions;
using Bmz.LabTests.Application.Abstractions.Auth;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер аутентификации.
/// Обеспечивает вход в систему и получение информации о текущем пользователе.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IIdentityProvider identityProvider,
    IUserRepository userRepository,
    ICurrentUserService currentUser) : ApiControllerBase
{
    /// <summary>
    /// Возвращает профиль текущего авторизованного пользователя.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        var user = await userRepository.FindByIdWithLaboratoryAsync(currentUser.UserId, cancellationToken);
        if (user is null)
            return NotFound();

        return Ok(new
        {
            fullName = user.FullName,
            role = user.Role.Name,
            laboratoryId = user.LaboratoryId,
            laboratoryName = user.Laboratory?.Name
        });
    }

    /// <summary>
    /// Выполняет аутентификацию пользователя и возвращает JWT токен.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await identityProvider.LoginAsync(request, cancellationToken));
    }
}
