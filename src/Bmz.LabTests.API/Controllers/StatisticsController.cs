using Bmz.LabTests.Application.Abstractions.Reporting;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Engineer}")]
public sealed class StatisticsController(IStatisticsService statisticsService) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] int? laboratoryId,
        [FromQuery] StatisticsTrendGroupBy groupBy = StatisticsTrendGroupBy.Day,
        CancellationToken cancellationToken = default)
    {
        if (fromUtc >= toUtc)
            return BadRequest("Параметр fromUtc должен быть раньше параметра toUtc.");
        if ((toUtc - fromUtc).TotalDays > 365)
            return BadRequest("Диапазон дат не может превышать 365 дней.");

        return ToActionResult(await statisticsService.GetAsync(fromUtc, toUtc, groupBy, laboratoryId, cancellationToken));
    }
}
