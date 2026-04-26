namespace Bmz.LabTests.API.Contracts.Organization;

public sealed class CreateAssistantByEngineerRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
