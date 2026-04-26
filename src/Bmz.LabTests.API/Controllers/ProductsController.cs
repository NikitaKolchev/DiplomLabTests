using Bmz.LabTests.Application.Abstractions;
using Bmz.LabTests.Application.Abstractions.Organization;
using Bmz.LabTests.Application.Abstractions.Products;
using Bmz.LabTests.Application.Abstractions.ReferenceData;
using Bmz.LabTests.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bmz.LabTests.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Engineer}")]
public sealed class ProductsController(
    IProductService service,
    ILaboratoryService laboratoryService,
    IReferenceDataService referenceDataService,
    ICurrentUserService currentUser) : ApiControllerBase
{
    [HttpGet("filters")]
    public async Task<IActionResult> GetFilters(CancellationToken cancellationToken)
    {
        var labsResult = await laboratoryService.GetLaboratoriesAsync(cancellationToken);
        var wireCodesResult = await referenceDataService.GetWireCodesAsync(null, cancellationToken);
        var customersResult = await referenceDataService.GetCustomersAsync(null, cancellationToken);

        if (labsResult.IsFailure) return ToActionResult(labsResult);
        if (wireCodesResult.IsFailure) return ToActionResult(wireCodesResult);
        if (customersResult.IsFailure) return ToActionResult(customersResult);

        return Ok(new
        {
            laboratories = labsResult.Value.Select(x => new { x.Id, x.Name }),
            wireCodes = wireCodesResult.Value.Select(x => new { x.Id, x.Code }),
            customers = customersResult.Value.Select(x => new { x.Id, x.Name })
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] int? laboratoryId,
        [FromQuery] int? wireCodeId,
        [FromQuery] int? customerId,
        [FromQuery] ProductStatusFilter? status,
        [FromQuery] string? sortBy,
        [FromQuery] bool sortDesc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == 0)
            return Unauthorized();

        return ToActionResult(await service.GetProductsAsync(
            currentUser.UserId,
            currentUser.Role,
            fromUtc,
            toUtc,
            laboratoryId,
            wireCodeId,
            customerId,
            status,
            sortBy,
            sortDesc,
            page,
            pageSize,
            cancellationToken));
    }
}
