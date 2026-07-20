using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Administrator console for login accounts: status, provisioning, lockout
/// recovery, enable/disable, password + MFA resets, and API role assignment.
/// Self-destructive actions (disabling yourself, dropping your own admin
/// role) are guarded in <see cref="IIdentityAccountService"/>.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "Administrator")]
public class UserAdminController : ControllerBase
{
    private readonly IIdentityAccountService _accounts;

    public UserAdminController(IIdentityAccountService accounts)
    {
        _accounts = accounts;
    }

    private Guid ActingUserId =>
        Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new ForbiddenAccessException("The access token carries no subject.");

    [HttpGet("{id:guid}/account")]
    public async Task<IActionResult> GetAccount(Guid id)
        => Ok(await _accounts.GetStatusAsync(id, HttpContext.RequestAborted));

    public record ProvisionAccountRequest(string Email, string? InitialPassword, bool SendInvite = true, string Role = "Requester");

    /// <summary>Creates a login account for an existing domain user (legacy/imported rows).</summary>
    [HttpPost("{id:guid}/provision-account")]
    public async Task<IActionResult> ProvisionAccount(Guid id, [FromBody] ProvisionAccountRequest request)
    {
        await _accounts.CreateAccountAsync(id, request.Email, request.InitialPassword, request.Role, request.SendInvite, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id)
    {
        await _accounts.UnlockAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id)
    {
        await _accounts.EnableAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id)
    {
        await _accounts.DisableAsync(id, ActingUserId, HttpContext.RequestAborted);
        return NoContent();
    }

    public record SetPasswordRequest([Required, MinLength(8)] string NewPassword);

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] SetPasswordRequest request)
    {
        await _accounts.SetPasswordAsync(id, request.NewPassword, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>Emails the user a self-service reset link instead of handing out a password.</summary>
    [HttpPost("{id:guid}/send-reset-email")]
    public async Task<IActionResult> SendResetEmail(Guid id)
    {
        var status = await _accounts.GetStatusAsync(id, HttpContext.RequestAborted);
        if (!status.HasAccount)
        {
            throw new NotFoundException("LoginAccount", id);
        }
        // Resolve the account's email through the domain user id.
        await _accounts.SendPasswordResetEmailByIdAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-mfa")]
    public async Task<IActionResult> ResetMfa(Guid id)
    {
        await _accounts.ResetMfaAsync(id, HttpContext.RequestAborted);
        return NoContent();
    }

    public record SetRolesRequest([Required] List<string> Roles);

    [HttpPut("{id:guid}/roles")]
    public async Task<IActionResult> SetRoles(Guid id, [FromBody] SetRolesRequest request)
    {
        await _accounts.SetRolesAsync(id, request.Roles, ActingUserId, HttpContext.RequestAborted);
        return NoContent();
    }
}
