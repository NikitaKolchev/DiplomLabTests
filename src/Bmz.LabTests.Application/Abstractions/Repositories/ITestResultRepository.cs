using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Domain.Enums;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface ITestResultRepository
{
    Task<(List<TestResult> Items, int TotalCount)> GetListAsync(Specification<TestResult> specification, CancellationToken cancellationToken);

    Task<TestResult?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<TestResult?> GetByIdWithValuesAsync(int id, CancellationToken cancellationToken);
    Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<List<int>> GetAllowedParameterIdsAsync(int wireCodeId, CancellationToken cancellationToken);

    Task AddAsync(TestResult testResult, CancellationToken cancellationToken);
    Task AddFinalProductAsync(FinalProduct finalProduct, CancellationToken cancellationToken);
    Task AddRejectAsync(Reject reject, CancellationToken cancellationToken);
    Task<List<WireCodeLimit>> GetLimitsByWireCodeIdAsync(int wireCodeId, CancellationToken cancellationToken);
    void SetOriginalRowVersion(TestResult testResult, byte[] rowVersion);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteByIdAsync(int id, CancellationToken cancellationToken);
}
