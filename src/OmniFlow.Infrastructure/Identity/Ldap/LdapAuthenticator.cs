using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novell.Directory.Ldap;

namespace OmniFlow.Infrastructure.Identity.Ldap;

public record LdapUserInfo(string Username, string? Email, string? DisplayName);

public interface ILdapAuthenticator
{
    bool Enabled { get; }
    LdapOptions Options { get; }

    /// <summary>
    /// Validates directory credentials. Returns the user's directory profile on
    /// success, null on any failure (invalid credentials, user not found,
    /// directory unreachable) — authentication fails closed.
    /// </summary>
    Task<LdapUserInfo?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default);
}

public class LdapAuthenticator : ILdapAuthenticator
{
    private readonly LdapOptions _options;
    private readonly ILogger<LdapAuthenticator> _logger;

    public LdapAuthenticator(IOptions<LdapOptions> options, ILogger<LdapAuthenticator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool Enabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.Host);
    public LdapOptions Options => _options;

    public Task<LdapUserInfo?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (!Enabled || string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return Task.FromResult<LdapUserInfo?>(null);
        }

        // Novell's connection API is synchronous; keep it off the request thread.
        return Task.Run(() =>
        {
            try
            {
                return !string.IsNullOrWhiteSpace(_options.BindDn)
                    ? SearchThenBind(username, password)
                    : DirectBind(username, password);
            }
            catch (LdapException ex)
            {
                _logger.LogWarning(ex, "LDAP authentication failed for {Username} (result code {Code}).", username, ex.ResultCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LDAP directory unreachable while authenticating {Username}.", username);
                return null;
            }
        }, cancellationToken);
    }

    private LdapUserInfo? SearchThenBind(string username, string password)
    {
        using var connection = Connect();
        connection.Bind(_options.BindDn, _options.BindPassword);

        var filter = string.Format(_options.SearchFilter, EscapeFilterValue(username));
        var results = connection.Search(
            _options.SearchBase ?? string.Empty,
            LdapConnection.ScopeSub,
            filter,
            new[] { _options.EmailAttribute, _options.DisplayNameAttribute },
            typesOnly: false);

        LdapEntry? entry = null;
        while (results.HasMore())
        {
            try
            {
                entry = results.Next();
                break;
            }
            catch (LdapReferralException)
            {
                // AD emits referrals for foreign partitions; skip them.
            }
        }

        if (entry is null)
        {
            _logger.LogWarning("LDAP user {Username} not found under {SearchBase}.", username, _options.SearchBase);
            return null;
        }

        var email = GetAttribute(entry, _options.EmailAttribute);
        var displayName = GetAttribute(entry, _options.DisplayNameAttribute);

        // Re-bind as the located user to verify the password.
        using var userConnection = Connect();
        userConnection.Bind(entry.Dn, password);

        return new LdapUserInfo(username, email, displayName);
    }

    private LdapUserInfo? DirectBind(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(_options.UserDnFormat))
        {
            _logger.LogError("LDAP is enabled but neither BindDn (search-then-bind) nor UserDnFormat (direct bind) is configured.");
            return null;
        }

        var userDn = string.Format(_options.UserDnFormat, username);
        using var connection = Connect();
        connection.Bind(userDn, password);

        // Read the user's own entry for profile attributes; non-fatal if denied.
        string? email = null, displayName = null;
        try
        {
            var results = connection.Search(
                userDn, LdapConnection.ScopeBase, "(objectClass=*)",
                new[] { _options.EmailAttribute, _options.DisplayNameAttribute }, typesOnly: false);
            if (results.HasMore())
            {
                var entry = results.Next();
                email = GetAttribute(entry, _options.EmailAttribute);
                displayName = GetAttribute(entry, _options.DisplayNameAttribute);
            }
        }
        catch (LdapException ex)
        {
            _logger.LogDebug(ex, "Could not read LDAP profile attributes for {UserDn}.", userDn);
        }

        return new LdapUserInfo(username, email, displayName);
    }

    private LdapConnection Connect()
    {
        var connection = new LdapConnection { SecureSocketLayer = _options.UseSsl };
        connection.Connect(_options.Host, _options.Port);
        if (_options.UseStartTls)
        {
            connection.StartTls();
        }
        return connection;
    }

    private static string? GetAttribute(LdapEntry entry, string name)
    {
        try
        {
            var value = entry.GetAttribute(name)?.StringValue;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (KeyNotFoundException)
        {
            return null;
        }
    }

    /// <summary>RFC 4515 filter escaping — the username is untrusted input.</summary>
    private static string EscapeFilterValue(string value)
    {
        var sb = new System.Text.StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(c switch
            {
                '\\' => "\\5c",
                '*' => "\\2a",
                '(' => "\\28",
                ')' => "\\29",
                '\0' => "\\00",
                _ => c.ToString()
            });
        }
        return sb.ToString();
    }
}
