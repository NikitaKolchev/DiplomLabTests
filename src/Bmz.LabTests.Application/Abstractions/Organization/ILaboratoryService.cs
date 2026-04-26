using Bmz.LabTests.Application.Organization;
using Bmz.LabTests.Domain.Common;

namespace Bmz.LabTests.Application.Abstractions.Organization;

public interface ILaboratoryService
{
    Task<Result<IReadOnlyCollection<LaboratorySummaryDto>>> GetLaboratoriesAsync(CancellationToken cancellationToken);
    Task<Result<LaboratorySummaryDto>> CreateLaboratoryAsync(int actorUserId, string? actorLogin, string name, int? engineerId, CancellationToken cancellationToken);
    Task<Result<LaboratorySummaryDto>> UpdateLaboratoryAsync(int actorUserId, string? actorLogin, int id, string name, int? engineerId, CancellationToken cancellationToken);
    Task<Result> DeleteLaboratoryAsync(int actorUserId, string? actorLogin, int id, CancellationToken cancellationToken);
    Task<Result<LaboratorySummaryDto>> AssignEngineerAsync(int actorUserId, string? actorLogin, int laboratoryId, int engineerId, CancellationToken cancellationToken);
}
