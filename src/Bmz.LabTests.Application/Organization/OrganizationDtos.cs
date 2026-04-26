namespace Bmz.LabTests.Application.Organization;

public sealed record UserSummaryDto(int Id, string FullName, string Login, string Role, int? LaboratoryId);

public sealed record LaboratorySummaryDto(int Id, string Name, int? EngineerId, string? EngineerName);
