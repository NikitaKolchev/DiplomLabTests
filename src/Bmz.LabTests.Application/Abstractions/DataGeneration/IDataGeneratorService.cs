using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.DataGeneration;

public interface IDataGeneratorService
{
    Task<Result<int>> GenerateTestResultsAsync(int count, CancellationToken cancellationToken);
}
