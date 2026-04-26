using Bmz.LabTests.Application.Abstractions.Repositories;
using Bmz.LabTests.Domain.Entities;
using Bmz.LabTests.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bmz.LabTests.Infrastructure.Repositories;

public sealed class ProtocolRepository(ApplicationDbContext dbContext) : IProtocolRepository
{
    public Task<WireCode?> GetWireCodeByIdAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == wireCodeId, cancellationToken);

    public Task<bool> WireCodeExistsAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.WireCodes.AnyAsync(x => x.Id == wireCodeId, cancellationToken);

    public Task<List<WireCodeLimit>> GetLimitsForWireCodeAsync(int wireCodeId, CancellationToken cancellationToken)
        => dbContext.Limits
            .AsNoTracking()
            .Include(x => x.Parameter)
            .Where(x => x.WireCodeId == wireCodeId)
            .ToListAsync(cancellationToken);

    public Task<List<Parameter>> GetParametersByIdsAsync(IReadOnlyCollection<int> parameterIds, CancellationToken cancellationToken)
        => dbContext.Parameters
            .Where(x => parameterIds.Contains(x.Id))
            .ToListAsync(cancellationToken);

    public async Task ReplaceLimitsAsync(int wireCodeId, IReadOnlyCollection<WireCodeLimit> limits, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Limits.Where(x => x.WireCodeId == wireCodeId).ToListAsync(cancellationToken);
        dbContext.Limits.RemoveRange(existing);
        dbContext.Limits.AddRange(limits);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
