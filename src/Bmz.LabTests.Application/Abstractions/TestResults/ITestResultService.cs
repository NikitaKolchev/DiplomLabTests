using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Application.TestResults;
using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.Abstractions.TestResults;

public interface ITestResultService
{
    Task<Result<PaginatedListDto<TestResultListItemDto>>> GetListAsync(
        int currentUserId,
        string currentRole,
        DateTime? fromUtc,
        DateTime? toUtc,
        int? wireCodeId,
        string? batchNumber,
        TestResultStatus? status,
        int page,
        int pageSize,
        TestResultSortBy? sortBy,
        bool? sortDesc,
        CancellationToken cancellationToken);

    Task<Result<CreatedTestResultDto>> CreateAsync(int currentUserId, string currentRole, CreateTestResultDto request, CancellationToken cancellationToken);
    Task<Result<TestResultDetailsDto>> GetByIdAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken);
    Task<Result<SavedTestResultDto>> SaveValuesAsync(int currentUserId, string currentRole, int id, SaveTestValuesDto request, CancellationToken cancellationToken);
    Task<Result<CompletionResult>> CompleteAsync(int currentUserId, string currentRole, int id, string rowVersionBase64, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(int currentUserId, string currentRole, int id, CancellationToken cancellationToken);
}
