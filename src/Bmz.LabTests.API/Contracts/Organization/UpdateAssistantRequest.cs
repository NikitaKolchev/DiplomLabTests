namespace Bmz.LabTests.API.Contracts.Organization;

public sealed class UpdateAssistantRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? Password { get; set; }
}
