using Bmz.LabTests.Application.Abstractions.Protocol;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер для формирования динамических схем протоколов испытаний.
/// Отвечает за предоставление списка необходимых параметров для ввода результатов.
/// </summary>
[ApiController]
[Route("api/wire-codes/{wireCodeId:int}/input-fields")]
[Authorize(Roles = "Admin,Engineer,Assistant")]
public sealed class ProtocolBuilderController(IProtocolService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает схему полей ввода (параметры, единицы измерения, требования) для конкретной марки проволоки.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInputFields(int wireCodeId, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetInputSchemaAsync(wireCodeId, cancellationToken));
    }
}
