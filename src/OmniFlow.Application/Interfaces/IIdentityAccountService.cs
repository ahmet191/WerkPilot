using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

public record AccountStatus(
    bool HasAccount,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    bool LockedOut,
    DateTimeOffset? LockoutEnd,
    bool Disabled,
    int AccessFailedCount,
    IReadOnlyList<string> Roles);

/// <summary>
/// Login-account (Identity) lifecycle for domain users. The Identity account
/// always shares the domain <c>User</c>'s id. Application handlers stay
/// persistence-agnostic; the Infrastructure implementation wraps UserManager.
/// </summary>
public interface IIdentityAccountService
{
    Task<AccountStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the login account for a domain user (same id). With no initial
    /// password the account is passwordless (SSO/LDAP or invite-reset only).
    /// <paramref name="sendInvite"/> emails a set-your-password link.
    /// </summary>
    Task CreateAccountAsync(Guid userId, string email, string? initialPassword, string identityRole, bool sendInvite, CancellationToken cancellationToken = default);

    Task UpdateEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default);
    Task EnableAsync(Guid userId, CancellationToken cancellationToken = default);
    Task DisableAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken = default);
    Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ResetMfaAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default);
    Task SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, Guid actingUserId, CancellationToken cancellationToken = default);

    /// <summary>Silently no-ops for unknown emails (no account enumeration).</summary>
    Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Admin-triggered reset link for a known account id.</summary>
    Task SendPasswordResetEmailByIdAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>Domain role → API-authorization (Identity) role.</summary>
public static class IdentityRoleMap
{
    public static string For(UserRole role) => role switch
    {
        UserRole.Administrator => "Administrator",
        UserRole.Agent => "ITAgent",
        _ => "Requester",
    };
}
