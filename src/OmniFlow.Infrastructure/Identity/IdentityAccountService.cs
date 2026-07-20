using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;

namespace OmniFlow.Infrastructure.Identity;

public class IdentityAccountService : IIdentityAccountService
{
    /// <summary>Sentinel LockoutEnd meaning "administratively disabled" (vs a timed lockout).</summary>
    private static readonly DateTimeOffset DisabledSentinel = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DisabledThreshold = DateTimeOffset.UtcNow.AddYears(100);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ITenantService _tenantService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IdentityAccountService> _logger;

    public IdentityAccountService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ITenantService tenantService,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<IdentityAccountService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantService = tenantService;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    private string SpaBaseUrl => (_configuration["App:SpaBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    public async Task<AccountStatus> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return new AccountStatus(false, false, false, false, null, false, 0, []);
        }

        var disabled = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DisabledThreshold;
        var lockedOut = !disabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
        var roles = await _userManager.GetRolesAsync(user);
        return new AccountStatus(
            true, user.EmailConfirmed, user.TwoFactorEnabled,
            lockedOut, user.LockoutEnd, disabled, user.AccessFailedCount, roles.ToList());
    }

    public async Task CreateAccountAsync(Guid userId, string email, string? initialPassword, string identityRole, bool sendInvite, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByIdAsync(userId.ToString()) != null)
        {
            throw new ConflictException("The user already has a login account.");
        }
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            throw new ConflictException($"A login account already exists for '{email}'.");
        }

        var account = new ApplicationUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = _tenantService.TenantId
        };

        var result = string.IsNullOrEmpty(initialPassword)
            ? await _userManager.CreateAsync(account)
            : await _userManager.CreateAsync(account, initialPassword);
        ThrowIfFailed(result, "creating the login account");

        await _userManager.AddToRoleAsync(account, identityRole);

        if (sendInvite)
        {
            await SendResetEmailCoreAsync(account, invitation: true, cancellationToken);
        }

        _logger.LogInformation("Login account created for {Email} ({UserId}) with role {Role}.", email, userId, identityRole);
    }

    public async Task UpdateEmailAsync(Guid userId, string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        ThrowIfFailed(await _userManager.SetEmailAsync(user, email), "updating the account email");
        ThrowIfFailed(await _userManager.SetUserNameAsync(user, email), "updating the account username");
    }

    public async Task EnableAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);
        ThrowIfFailed(await _userManager.SetLockoutEndDateAsync(user, null), "enabling the account");
        await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task DisableAsync(Guid userId, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        if (userId == actingUserId)
        {
            throw new ValidationException("userId", "You cannot disable your own account.");
        }
        var user = await RequireAccountAsync(userId);
        await _userManager.SetLockoutEnabledAsync(user, true);
        ThrowIfFailed(await _userManager.SetLockoutEndDateAsync(user, DisabledSentinel), "disabling the account");
    }

    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);
        var disabled = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DisabledThreshold;
        if (disabled)
        {
            throw new ConflictException("The account is administratively disabled; enable it instead.");
        }
        ThrowIfFailed(await _userManager.SetLockoutEndDateAsync(user, null), "unlocking the account");
        await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task ResetMfaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);
        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        _logger.LogInformation("MFA reset for account {UserId}.", userId);
    }

    public async Task SetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        ThrowIfFailed(await _userManager.ResetPasswordAsync(user, token, newPassword), "setting the password");
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task SetRolesAsync(Guid userId, IReadOnlyCollection<string> roles, Guid actingUserId, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                throw new ValidationException("roles", $"Unknown role '{role}'.");
            }
        }

        if (userId == actingUserId && !roles.Contains("Administrator"))
        {
            throw new ValidationException("roles", "You cannot remove the Administrator role from your own account.");
        }

        var current = await _userManager.GetRolesAsync(user);
        var toRemove = current.Except(roles).ToList();
        var toAdd = roles.Except(current).ToList();
        if (toRemove.Count > 0) ThrowIfFailed(await _userManager.RemoveFromRolesAsync(user, toRemove), "updating roles");
        if (toAdd.Count > 0) ThrowIfFailed(await _userManager.AddToRolesAsync(user, toAdd), "updating roles");
    }

    public async Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogInformation("Password reset requested for unknown email; silently ignored.");
            return;
        }
        await SendResetEmailCoreAsync(user, invitation: false, cancellationToken);
    }

    public async Task SendPasswordResetEmailByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await RequireAccountAsync(userId);
        await SendResetEmailCoreAsync(user, invitation: false, cancellationToken);
    }

    // ── Private helpers ──

    private async Task SendResetEmailCoreAsync(ApplicationUser user, bool invitation, CancellationToken cancellationToken)
    {
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        // Fragment keeps the token out of server/proxy logs.
        var link = $"{SpaBaseUrl}/reset-password#email={WebUtility.UrlEncode(user.Email)}&token={WebUtility.UrlEncode(token)}";

        var (subject, headline, action) = invitation
            ? ("Welcome to WerkPilot — set your password", "Your WerkPilot account is ready.", "Set my password")
            : ("WerkPilot password reset", "We received a request to reset your password.", "Reset my password");

        var html = $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px">
              <h2 style="color:#0f172a;margin:0 0 4px">WerkPilot</h2>
              <p style="color:#334155">{headline}</p>
              <p style="margin:24px 0">
                <a href="{link}" style="background:#0ea5e9;color:#ffffff;padding:10px 18px;border-radius:8px;text-decoration:none">{action}</a>
              </p>
              <p style="color:#64748b;font-size:13px">If the button does not work, copy this link:<br>{link}</p>
              <p style="color:#94a3b8;font-size:12px">If you did not expect this email, you can safely ignore it.</p>
            </div>
            """;

        try
        {
            await _emailSender.SendAsync(user.Email!, subject, html, cancellationToken);
        }
        catch (Exception ex)
        {
            // Email delivery is best-effort: a down SMTP relay must not break
            // account creation or leak infrastructure details to callers. The
            // admin console's "send reset email" can simply be retried.
            _logger.LogError(ex, "Failed to send {Kind} email to {Email}.", invitation ? "invitation" : "password-reset", user.Email);
        }
    }

    private async Task<ApplicationUser> RequireAccountAsync(Guid userId)
        => await _userManager.FindByIdAsync(userId.ToString())
           ?? throw new NotFoundException("LoginAccount", userId);

    private static void ThrowIfFailed(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new ValidationException("account",
                $"Identity error while {operation}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
