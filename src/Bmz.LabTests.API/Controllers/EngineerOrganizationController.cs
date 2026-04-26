using Bmz.LabTests.API.Contracts.Organization;
using Bmz.LabTests.API.Extensions;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/engineer")]
[Authorize(Roles = Roles.Engineer)]
public sealed class EngineerOrganizationController(IUserManagementService userManagementService) : ApiControllerBase
{
    [HttpGet("users/assistants")]
    public async Task<IActionResult> GetAssistants([FromQuery] string? search, [FromQuery] string? login, CancellationToken cancellationToken)
    {
        var (engineerUserId, _) = this.GetCurrentActor();
        if (engineerUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.GetAssistantsForEngineerAsync(engineerUserId, search, login, cancellationToken));
    }

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
