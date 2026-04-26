using Bmz.LabTests.Application.Testing;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Testing;

public interface ITestResultCompletionService
{
    Task<Result<CompletionResult>> CompleteAsync(int testResultId, byte[] rowVersion, CancellationToken cancellationToken);
}
