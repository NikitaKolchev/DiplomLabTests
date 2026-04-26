using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Extensions;

public static class ControllerBaseExtensions
{
    /// <summary>
    /// Gets the current authenticated user's ID and login from JWT claims.
    /// Returns (0, null) if not authenticated or claims are invalid.
    /// </summary>
    public static (int UserId, string? Login) GetCurrentActor(this ControllerBase controller)
    {
        if (controller.User.Identity?.IsAuthenticated != true)
            return (0, null);

        var userIdClaim = controller.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? controller.User.FindFirstValue("sub");
        var login = controller.User.FindFirstValue(ClaimTypes.Name) ?? controller.User.Identity?.Name;

        return int.TryParse(userIdClaim, out var userId) ? (userId, login) : (0, null);
    }
}
