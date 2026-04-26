using Bmz.LabTests.Application.Abstractions.Products;
using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Constants;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Products;

public sealed class ProductService(
    ApplicationDbContext dbContext,
    ITestResultRepository testResultRepository) : IProductService
{
    public async Task<Result<PaginatedListDto<ProductListItemDto>>> GetProductsAsync(
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
        CancellationToken cancellationToken)
    {
        var laboratoryIdFilter = await ResolveLaboratoryScopeAsync(currentUserId, currentRole, cancellationToken);
        if (laboratoryIdFilter.HasValue && !laboratoryId.HasValue)
            laboratoryId = laboratoryIdFilter;

        var query =
            from x in dbContext.TestResults
            .AsNoTracking()
            .Where(x => x.Status == Domain.Enums.TestResultStatus.Completed)
            join reject in dbContext.Rejects.AsNoTracking() on x.Id equals reject.TestResultId into rejectGroup
            from reject in rejectGroup.DefaultIfEmpty()
            select new
            {
                x.Id,
                x.Date,
                x.BatchNumber,
                WireCode = x.WireCode.Code,
                Laboratory = x.Laboratory.Name,
                CustomerName = x.Customer != null ? x.Customer.Name : null,
                Assistant = x.Assistant.FullName,
                IsRejected = reject != null,
                RejectReason = reject != null ? reject.Reason : null,
                x.LaboratoryId,
                x.WireCodeId,
                x.CustomerId
            };

        if (laboratoryId.HasValue)
            query = query.Where(x => x.LaboratoryId == laboratoryId.Value);
        if (fromUtc.HasValue)
            query = query.Where(x => x.Date >= fromUtc.Value);
        if (toUtc.HasValue)
            query = query.Where(x => x.Date <= toUtc.Value);
        if (wireCodeId.HasValue)
            query = query.Where(x => x.WireCodeId == wireCodeId.Value);
        if (customerId.HasValue)
            query = query.Where(x => x.CustomerId == customerId.Value);

        query = status switch
        {
            ProductStatusFilter.Accepted => query.Where(x => !x.IsRejected),
            ProductStatusFilter.Rejected => query.Where(x => x.IsRejected),
            _ => query
        };

        query = sortBy?.ToLowerInvariant() switch
        {
            "date" => sortDesc ? query.OrderByDescending(x => x.Date) : query.OrderBy(x => x.Date),
            "batchnumber" or "batch" => sortDesc ? query.OrderByDescending(x => x.BatchNumber) : query.OrderBy(x => x.BatchNumber),
            "wirecode" or "wire" => sortDesc ? query.OrderByDescending(x => x.WireCode) : query.OrderBy(x => x.WireCode),
            "laboratory" or "lab" => sortDesc ? query.OrderByDescending(x => x.Laboratory) : query.OrderBy(x => x.Laboratory),
            "customer" or "customername" => sortDesc ? query.OrderByDescending(x => x.CustomerName ?? "") : query.OrderBy(x => x.CustomerName ?? ""),
            "assistant" => sortDesc ? query.OrderByDescending(x => x.Assistant) : query.OrderBy(x => x.Assistant),
            "status" or "isaccepted" => sortDesc ? query.OrderByDescending(x => x.IsRejected) : query.OrderBy(x => x.IsRejected),
            _ => query.OrderByDescending(x => x.Date)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductListItemDto(
                x.Id,
                x.Date,
                x.BatchNumber,
                x.WireCode,
                x.Laboratory,
                x.CustomerName,
                x.Assistant,
                !x.IsRejected,
                x.RejectReason))
            .ToArrayAsync(cancellationToken);

        return Result.Success(new PaginatedListDto<ProductListItemDto>(items, totalCount, page, pageSize, totalPages));
    }

    private async Task<int?> ResolveLaboratoryScopeAsync(int currentUserId, string currentRole, CancellationToken cancellationToken)
    {
        if (string.Equals(currentRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            return null;

        if (!string.Equals(currentRole, Roles.Engineer, StringComparison.OrdinalIgnoreCase))
            return null;

        var user = await testResultRepository.GetUserByIdAsync(currentUserId, cancellationToken);
        var labId = user?.LaboratoryId ?? await testResultRepository.GetLaboratoryIdByEngineerIdAsync(currentUserId, cancellationToken);
        return labId;
    }
}
