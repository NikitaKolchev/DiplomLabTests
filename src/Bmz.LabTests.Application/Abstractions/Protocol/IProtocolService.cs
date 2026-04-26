using Bmz.LabTests.Application.Protocol;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Protocol;

public interface IProtocolService
{
    Task<Result<WireCodeInputSchemaDto>> GetInputSchemaAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<LimitDto>>> GetLimitsAsync(int wireCodeId, CancellationToken cancellationToken);
    Task<Result> ReplaceLimitsAsync(int actorUserId, string? actorLogin, int wireCodeId, IReadOnlyCollection<LimitUpsertItemDto> items, CancellationToken cancellationToken);
}
