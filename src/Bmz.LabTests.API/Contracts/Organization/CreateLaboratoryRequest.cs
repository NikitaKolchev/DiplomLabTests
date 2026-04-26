namespace Bmz.LabTests.API.Contracts.Organization;

public sealed class CreateLaboratoryRequest
{
    public string Name { get; set; } = string.Empty;
    public int? EngineerId { get; set; }
}
