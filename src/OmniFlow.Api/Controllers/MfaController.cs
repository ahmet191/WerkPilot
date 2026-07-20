using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Infrastructure.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Authenticator-app (TOTP) multi-factor management for the current user.
/// Enrollment: POST setup → scan QR → POST enable with a live code →
/// store the returned recovery codes.
/// </summary>
[ApiController]
[Route("api/account/mfa")]
public class MfaController : ControllerBase
{
    private const string Issuer = "WerkPilot";

    private readonly UserManager<ApplicationUser> _userManager;

    public MfaController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        var user = await GetCurrentUserAsync();
        return Ok(new
        {
            enabled = user.TwoFactorEnabled,
            recoveryCodesLeft = user.TwoFactorEnabled ? await _userManager.CountRecoveryCodesAsync(user) : 0
        });
    }

    /// <summary>Starts (or restarts) enrollment by rotating the authenticator key.</summary>
    [HttpPost("setup")]
    public async Task<IActionResult> Setup()
    {
        var user = await GetCurrentUserAsync();

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new InvalidOperationException("Authenticator key generation failed.");

        var accountLabel = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        return Ok(new
        {
            sharedKey = FormatKey(key),
            otpauthUri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{accountLabel}?secret={key}&issuer={Uri.EscapeDataString(Issuer)}&digits=6"
        });
    }

    public record VerifyCodeRequest(string Code);

    /// <summary>Verifies a live TOTP code and turns MFA on; returns one-time recovery codes.</summary>
    [HttpPost("enable")]
    public async Task<IActionResult> Enable([FromBody] VerifyCodeRequest request)
    {
        var user = await GetCurrentUserAsync();

        if (!await VerifyTotpAsync(user, request.Code))
        {
            throw new ValidationException("code", "The verification code is invalid.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

        return Ok(new { recoveryCodes });
    }

    /// <summary>Turns MFA off after re-verifying a live code (or recovery code).</summary>
    [HttpPost("disable")]
    public async Task<IActionResult> Disable([FromBody] VerifyCodeRequest request)
    {
        var user = await GetCurrentUserAsync();

        // Recovery codes keep their canonical xxxxx-xxxxx shape — only trim.
        var valid = await VerifyTotpAsync(user, request.Code)
                    || (await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.Code.Trim())).Succeeded;
        if (!valid)
        {
            throw new ValidationException("code", "The verification code is invalid.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        return NoContent();
    }

    // ── Private helpers ──

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(Claims.Subject)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ForbiddenAccessException("The access token carries no subject.");
        return await _userManager.FindByIdAsync(userId)
               ?? throw new NotFoundException(nameof(ApplicationUser), userId);
    }

    private async Task<bool> VerifyTotpAsync(ApplicationUser user, string code)
        => !string.IsNullOrWhiteSpace(code)
           && await _userManager.VerifyTwoFactorTokenAsync(
               user, _userManager.Options.Tokens.AuthenticatorTokenProvider, Normalize(code));

    private static string Normalize(string code)
        => code.Replace(" ", string.Empty).Replace("-", string.Empty);

    private static string FormatKey(string key)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < key.Length; i += 4)
        {
            sb.Append(key.AsSpan(i, Math.Min(4, key.Length - i))).Append(' ');
        }
        return sb.ToString().TrimEnd().ToLowerInvariant();
    }
}
