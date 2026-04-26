using Bmz.LabTests.Domain.Common;
using Bmz.LabTests.Infrastructure.Persistence;

namespace Bmz.LabTests.Infrastructure.Persistence.Seeding;

public interface IEntitySeeder
{
    int Order { get; }
    Task<Result> SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken);
}