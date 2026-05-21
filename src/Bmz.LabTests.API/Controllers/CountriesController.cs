using Bmz.LabTests.API.Contracts.Countries;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер справочника стран.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CountriesController(IReferenceDataService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех стран.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetCountriesAsync(searchTerm, cancellationToken));
    }

    /// <summary>
    /// Возвращает информацию о стране по ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetCountryByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Добавляет новую страну в справочник.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] UpsertCountryRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateCountryAsync(request.Name, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Обновляет данные страны.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertCountryRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.UpdateCountryAsync(id, request.Name, cancellationToken));
    }

    /// <summary>
    /// Удаляет страну из справочника.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteCountryAsync(id, cancellationToken));
    }
}
