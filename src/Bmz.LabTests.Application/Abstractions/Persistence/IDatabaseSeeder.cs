using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Persistence;

public interface IDatabaseSeeder
{
    Task<Result> SeedAsync(CancellationToken cancellationToken = default);
}
