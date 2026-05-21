using Bmz.LabTests.API.Contracts.Parameters;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер глобального справочника параметров испытаний.
/// Позволяет определять типы измерений (например, Диаметр, Временное сопротивление).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ParametersController(IReferenceDataService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех доступных параметров.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetParametersAsync(searchTerm, cancellationToken));
    }

    /// <summary>
    /// Возвращает данные параметра по ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetParameterByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Создает новый параметр испытания.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> Create([FromBody] UpsertParameterRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateParameterAsync(request.Name, request.DataType, request.Unit, cancellationToken);
        if (result.IsFailure) return ToActionResult(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Обновляет данные параметра.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Engineer")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertParameterRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.UpdateParameterAsync(id, request.Name, request.DataType, request.Unit, cancellationToken));
    }

    /// <summary>
    /// Удаляет параметр из справочника.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteParameterAsync(id, cancellationToken));
    }
}
