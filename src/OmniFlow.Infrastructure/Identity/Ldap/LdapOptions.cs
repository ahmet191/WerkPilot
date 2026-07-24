namespace OmniFlow.Infrastructure.Identity.Ldap;

/// <summary>
/// Optional LDAP / Active Directory authentication ("Ldap" config section).
/// Two bind strategies are supported:
///  - Service-account search-then-bind: set <see cref="BindDn"/> +
///    <see cref="BindPassword"/> + <see cref="SearchBase"/>; the user is
///    located with <see cref="SearchFilter"/> and re-bound with their own
///    password.
///  - Direct bind: set <see cref="UserDnFormat"/> (e.g.
///    "uid={0},ou=people,dc=corp,dc=example" or "{0}@corp.example" for AD UPN).
/// </summary>
public class LdapOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public bool UseSsl { get; set; }
    public bool UseStartTls { get; set; }

    public string? BindDn { get; set; }
    public string? BindPassword { get; set; }
    public string? SearchBase { get; set; }
    public string SearchFilter { get; set; } = "(|(uid={0})(sAMAccountName={0})(mail={0}))";
    public string? UserDnFormat { get; set; }

    public string EmailAttribute { get; set; } = "mail";
    public string DisplayNameAttribute { get; set; } = "cn";

    /// <summary>Create a local account on first successful LDAP login.</summary>
    public bool AutoProvision { get; set; }
    public string DefaultRole { get; set; } = "Requester";

    /// <summary>Used when the directory entry has no email attribute.</summary>
    public string FallbackEmailDomain { get; set; } = "ldap.local";
}
