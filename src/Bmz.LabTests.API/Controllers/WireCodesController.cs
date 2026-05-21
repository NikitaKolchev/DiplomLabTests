using Bmz.LabTests.API.Contracts.WireCodes;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер для управления справочником марок проволоки (кодов продукции).
/// Позволяет просматривать, создавать, обновлять и удалять типы выпускаемой продукции.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class WireCodesController(IReferenceDataService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех марок проволоки с возможностью поиска по коду или маркировке.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetWireCodesAsync(searchTerm, cancellationToken));
    }

    /// <summary>
    /// Получает детальную информацию о марке проволоки по её идентификатору.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetWireCodeByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Создает новую марку проволоки в справочнике. Доступно только администратору.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Create([FromBody] UpsertWireCodeRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateWireCodeAsync(request.Code, request.Marking, request.Diameter, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Обновляет данные существующей марки проволоки. Доступно только администратору.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertWireCodeRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.UpdateWireCodeAsync(id, request.Code, request.Marking, request.Diameter, cancellationToken));
    }

    /// <summary>
    /// Удаляет марку проволоки из справочника. Доступно только администратору.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteWireCodeAsync(id, cancellationToken));
    }
}
