using Bmz.LabTests.Application.Abstractions.DataGeneration;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize(Roles = Roles.Admin)]
public sealed class MaintenanceController(IDataGeneratorService dataGeneratorService) : ApiControllerBase
{
    [HttpPost("generate-data")]
    public async Task<IActionResult> GenerateData([FromQuery] int count, CancellationToken cancellationToken)
    {
        if (count <= 0)
            return BadRequest("Количество записей должно быть больше нуля.");

        if (count > 100000)
            return BadRequest("Максимальное количество за один раз - 100 000.");

        var result = await dataGeneratorService.GenerateTestResultsAsync(count, cancellationToken);
        
        return ToActionResult(result);
    }
}
