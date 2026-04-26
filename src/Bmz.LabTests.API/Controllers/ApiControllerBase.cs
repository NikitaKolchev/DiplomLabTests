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

    private IActionResult MapError(Error error)
    {
        return error.Type switch
        {
            ErrorType.None => StatusCode(500),
            ErrorType.NotFound => NotFound(error.Message),
            ErrorType.Forbidden => Forbid(),
            ErrorType.Unauthorized => Unauthorized(),
            ErrorType.Conflict => Conflict(error.Message),
            ErrorType.Validation => BadRequest(error.Message),
            ErrorType.Failure => BadRequest(error.Message),
            _ => BadRequest(error.Message)
        };
    }
}