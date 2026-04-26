using Bmz.LabTests.API.Contracts.WireCodes;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class WireCodesController(IReferenceDataService service) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetWireCodesAsync(searchTerm, cancellationToken));
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetWireCodeByIdAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] UpsertWireCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateWireCodeAsync(request.Code, request.Marking, request.Diameter, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertWireCodeRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.UpdateWireCodeAsync(id, request.Code, request.Marking, request.Diameter, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteWireCodeAsync(id, cancellationToken));
    }
}
