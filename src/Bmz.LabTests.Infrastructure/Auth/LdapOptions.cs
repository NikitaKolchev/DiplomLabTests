namespace Bmz.LabTests.Infrastructure.Auth;

/// <summary>
/// Параметры подключения к серверу Active Directory (LDAP).
/// </summary>
public sealed class LdapOptions
{
    public const string SectionName = "Ldap";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 389;

    public bool UseSsl { get; set; }

    public string Domain { get; set; } = string.Empty;

    public string SearchBase { get; set; } = string.Empty;

    public string SearchFilter { get; set; } = "(sAMAccountName={0})";

    public string ServiceUser { get; set; } = string.Empty;

    public string ServicePassword { get; set; } = string.Empty;
}
