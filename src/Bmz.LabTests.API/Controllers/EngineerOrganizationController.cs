using Bmz.LabTests.API.Contracts.Organization;
using Bmz.LabTests.API.Extensions;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер для инженеров по управлению персоналом своей лаборатории.
/// Позволяет инженеру просматривать и редактировать лаборантов, закрепленных за его подразделением.
/// </summary>
[ApiController]
[Route("api/engineer")]
[Authorize(Roles = Roles.Engineer)]
public sealed class EngineerOrganizationController(IUserManagementService userManagementService) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список лаборантов текущей лаборатории.
    /// </summary>
    [HttpGet("users/assistants")]
    public async Task<IActionResult> GetAssistants([FromQuery] string? search, [FromQuery] string? login, CancellationToken cancellationToken)
    {
        var (engineerUserId, _) = this.GetCurrentActor();
        if (engineerUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.GetAssistantsForEngineerAsync(engineerUserId, search, login, cancellationToken));
    }

    /// <summary>
    /// Создает новую учетную запись лаборанта в текущей лаборатории.
    /// </summary>
    [HttpPost("users/assistants")]
    public async Task<IActionResult> CreateAssistant([FromBody] CreateAssistantByEngineerRequest request, CancellationToken cancellationToken)
    {
        var (engineerUserId, actorLogin) = this.GetCurrentActor();
        if (engineerUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.CreateAssistantByEngineerAsync(
            engineerUserId,
            actorLogin,
            request.FullName,
            request.Login,
            request.Password,
            cancellationToken));
    }

    /// <summary>
    /// Обновляет данные лаборанта текущей лаборатории.
    /// </summary>
    [HttpPut("users/assistants/{assistantId:int}")]
    public async Task<IActionResult> UpdateAssistant(int assistantId, [FromBody] UpdateAssistantRequest request, CancellationToken cancellationToken)
    {
        var (engineerUserId, _) = this.GetCurrentActor();
        if (engineerUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.UpdateAssistantByEngineerAsync(
            engineerUserId,
            assistantId,
            request.FullName,
            request.Login,
            request.Password,
            cancellationToken));
    }
}
