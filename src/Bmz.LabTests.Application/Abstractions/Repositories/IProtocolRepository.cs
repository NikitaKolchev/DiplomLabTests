using Bmz.LabTests.Domain.Entities;

namespace Bmz.LabTests.Application.Abstractions.Repositories;

public interface IProtocolRepository
{
    Task<WireCode?> GetWireCodeByIdAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<List<WireCodeLimit>> GetLimitsForWireCodeAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<List<Parameter>> GetParametersByIdsAsync(IReadOnlyCollection<int> parameterIds, CancellationToken cancellationToken);
    Task ReplaceLimitsAsync(int wireCodeId, IReadOnlyCollection<WireCodeLimit> limits, CancellationToken cancellationToken);
}
