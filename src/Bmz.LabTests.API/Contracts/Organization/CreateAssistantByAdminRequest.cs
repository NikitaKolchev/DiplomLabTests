namespace Bmz.LabTests.API.Contracts.Organization;

public sealed class CreateAssistantByAdminRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int LaboratoryId { get; set; }
}
