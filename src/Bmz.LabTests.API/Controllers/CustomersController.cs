using Bmz.LabTests.API.Contracts.Customers;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bmz.LabTests.API.Controllers;

/// <summary>
/// Контроллер справочника потребителей (заказчиков).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CustomersController(IReferenceDataService service) : ApiControllerBase
{
    /// <summary>
    /// Возвращает список всех потребителей.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetCustomersAsync(searchTerm, cancellationToken));
    }

    /// <summary>
    /// Возвращает информацию о потребителе по ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.GetCustomerByIdAsync(id, cancellationToken));
    }

    /// <summary>
    /// Регистрирует нового потребителя в системе.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateCustomerAsync(request.Name, request.CountryId, cancellationToken);
        if (result.IsFailure)
            return ToActionResult(result);

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value);
    }

    /// <summary>
    /// Обновляет данные потребителя.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertCustomerRequest request, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.UpdateCustomerAsync(id, request.Name, request.CountryId, cancellationToken));
    }

    /// <summary>
    /// Удаляет потребителя из справочника.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        return ToActionResult(await service.DeleteCustomerAsync(id, cancellationToken));
    }
}
