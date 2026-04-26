using Bmz.LabTests.Application.Abstractions.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/wire-codes/{wireCodeId:int}/input-fields")]
[Authorize(Roles = "Admin,Engineer,Assistant")]
public sealed class ProtocolBuilderController(IProtocolService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetInputFields(int wireCodeId, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetInputSchemaAsync(wireCodeId, cancellationToken));
    }
}
