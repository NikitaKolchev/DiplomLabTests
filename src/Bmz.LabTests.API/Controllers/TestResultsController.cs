using Bmz.LabTests.API.Contracts.TestResults;
using Bmz.LabTests.Application.Abstractions;
using Bmz.LabTests.Application.Abstractions.TestResults;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TestResultsController(
    ITestResultService service,
    ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? wireCodeId,
        [FromQuery] string? batchNumber,
        [FromQuery] TestResultStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? sortDesc = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        return ToActionResult(await service.GetListAsync(
            currentUser.UserId,
            currentUser.Role,
            fromUtc,
            toUtc,
            wireCodeId,
            batchNumber,
            status,
            page,
            pageSize,
            sortBy,
            sortDesc,
            cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Assistant)]
    public async Task<IActionResult> Create([FromBody] CreateTestResultRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        var result = await service.CreateAsync(
            currentUser.UserId,
            currentUser.Role,
            new Bmz.LabTests.Application.TestResults.CreateTestResultDto(
                currentUser.UserId,
                request.WireCodeId,
                request.BatchNumber,
                request.CustomerId),
            cancellationToken);

        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(currentUser.UserId, currentUser.Role, id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:int}/values")]
    [Authorize(Roles = $"{Roles.Assistant},{Roles.Engineer},{Roles.Admin}")]
    public async Task<IActionResult> SaveValues(int id, [FromBody] SaveTestValuesRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        try
        {
            var result = await service.SaveValuesAsync(
                currentUser.UserId,
                currentUser.Role,
                id,
                new Bmz.LabTests.Application.TestResults.SaveTestValuesDto(
                    request.RowVersion,
                    request.Values.Select(x => new Bmz.LabTests.Application.TestResults.SaveValueItemDto(x.ParameterId, x.Value)).ToArray()),
                cancellationToken);

            return ToActionResult(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Протокол обновлен другим пользователем. Обновите страницу и повторите попытку.");
        }
    }

    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = $"{Roles.Assistant},{Roles.Engineer},{Roles.Admin}")]
    public async Task<IActionResult> Complete(int id, [FromBody] CompleteTestResultRequest request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        try
        {
            var result = await service.CompleteAsync(currentUser.UserId, currentUser.Role, id, request.RowVersion, cancellationToken);
            return ToActionResult(result);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("Протокол обновлен другим пользователем. Обновите страницу и повторите попытку.");
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        var result = await service.DeleteAsync(currentUser.UserId, currentUser.Role, id, cancellationToken);
        return ToActionResult(result);
    }
}
