using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Products;

public interface IProductService
{
    Task<Result<PaginatedListDto<ProductListItemDto>>> GetProductsAsync(
        int currentUserId,
        string currentRole,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? laboratoryId,
        int? wireCodeId,
        int? customerId,
        ProductStatusFilter? status,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}

public enum ProductStatusFilter
{
    All,
    Accepted,
    Rejected
}
