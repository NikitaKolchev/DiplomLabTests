using Bmz.LabTests.API.Contracts.Organization;
using Bmz.LabTests.API.Extensions;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = Roles.Admin)]
public sealed class AdminOrganizationController(
    IUserManagementService userManagementService,
    ILaboratoryService laboratoryService) : ApiControllerBase
{
    [HttpGet("users/engineers")]
    public async Task<IActionResult> GetEngineers(CancellationToken cancellationToken)
        => ToActionResult(await userManagementService.GetEngineersAsync(cancellationToken));

    [HttpPut("users/{id:int}/role")]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateUserRoleRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.UpdateUserRoleAsync(actorUserId, actorLogin, id, request.RoleName, cancellationToken));
    }

    [HttpPost("users/engineers")]
    public async Task<IActionResult> CreateEngineer([FromBody] CreateEngineerRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.CreateEngineerAsync(actorUserId, actorLogin, request.FullName, request.Login, request.Password, request.LaboratoryId, cancellationToken));
    }

    [HttpPost("users/assistants")]
    public async Task<IActionResult> CreateAssistant([FromBody] CreateAssistantByAdminRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.CreateAssistantByAdminAsync(
            actorUserId,
            actorLogin,
            request.FullName,
            request.Login,
            request.Password,
            request.LaboratoryId,
            cancellationToken));
    }

    [HttpGet("laboratories")]
    public async Task<IActionResult> GetLaboratories(CancellationToken cancellationToken)
        => ToActionResult(await laboratoryService.GetLaboratoriesAsync(cancellationToken));

    [HttpPost("laboratories")]
    public async Task<IActionResult> CreateLaboratory([FromBody] CreateLaboratoryRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await laboratoryService.CreateLaboratoryAsync(actorUserId, actorLogin, request.Name, request.EngineerId, cancellationToken));
    }

    [HttpPut("laboratories/{laboratoryId:int}/engineer")]
    public async Task<IActionResult> AssignEngineer(int laboratoryId, [FromBody] AssignEngineerRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await laboratoryService.AssignEngineerAsync(actorUserId, actorLogin, laboratoryId, request.EngineerId, cancellationToken));
    }

    [HttpPut("laboratories/{id:int}")]
    public async Task<IActionResult> UpdateLaboratory(int id, [FromBody] UpdateLaboratoryRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await laboratoryService.UpdateLaboratoryAsync(actorUserId, actorLogin, id, request.Name, request.EngineerId, cancellationToken));
    }

    [HttpDelete("laboratories/{id:int}")]
    public async Task<IActionResult> DeleteLaboratory(int id, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await laboratoryService.DeleteLaboratoryAsync(actorUserId, actorLogin, id, cancellationToken));
    }

    [HttpPut("users/engineers/{id:int}")]
    public async Task<IActionResult> UpdateEngineer(int id, [FromBody] UpdateEngineerRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.UpdateEngineerAsync(actorUserId, actorLogin, id, request.FullName, request.Login, request.Password, request.LaboratoryId, cancellationToken));
    }

    [HttpDelete("users/engineers/{id:int}")]
    public async Task<IActionResult> DeleteEngineer(int id, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.DeleteEngineerAsync(actorUserId, actorLogin, id, cancellationToken));
    }

    [HttpGet("users/assistants")]
    public async Task<IActionResult> GetAssistants(CancellationToken cancellationToken)
        => ToActionResult(await userManagementService.GetAssistantsForAdminAsync(cancellationToken));

    [HttpPut("users/assistants/{id:int}")]
    public async Task<IActionResult> UpdateAssistant(int id, [FromBody] UpdateAssistantByAdminRequest request, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.UpdateAssistantByAdminAsync(
            actorUserId,
            actorLogin,
            id,
            request.FullName,
            request.Login,
            request.Password,
            request.LaboratoryId,
            cancellationToken));
    }

    [HttpDelete("users/assistants/{id:int}")]
    public async Task<IActionResult> DeleteAssistant(int id, CancellationToken cancellationToken)
    {
        var (actorUserId, actorLogin) = this.GetCurrentActor();
        if (actorUserId == 0) return Unauthorized();

        return ToActionResult(await userManagementService.DeleteAssistantAsync(actorUserId, actorLogin, id, cancellationToken));
    }

    [HttpGet("utils/transliterate")]
    public IActionResult TransliterateLogin([FromQuery] string fullName)
        => Ok(userManagementService.TransliterateLogin(fullName));

    [HttpGet("utils/password")]
    public IActionResult GeneratePassword([FromQuery] int length = 10)
        => Ok(userManagementService.GeneratePassword(length));
}
