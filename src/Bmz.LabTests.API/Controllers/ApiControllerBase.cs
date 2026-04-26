using Bmz.LabTests.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
            return NoContent();

        return MapError(result.Error);
    }

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        return MapError(result.Error);
    }

    private IActionResult MapError(string? error)
    {
        return error switch
        {
            null => StatusCode(500),
            var e when e.Contains("не найден", StringComparison.OrdinalIgnoreCase) => NotFound(e),
            var e when e.Contains("запрещен", StringComparison.OrdinalIgnoreCase) => Forbid(),
            var e when e.Contains("обновлен другим пользователем", StringComparison.OrdinalIgnoreCase) => Conflict(e),
            var e when e.Contains("Завершенный", StringComparison.OrdinalIgnoreCase) => Conflict(e),
            _ => BadRequest(error)
        };
    }
}