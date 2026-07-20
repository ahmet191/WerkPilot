using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Infrastructure.Identity;
using static OpenIddict.Abstractions.OpenIddictConstants;
using ValidationException = OmniFlow.Domain.Common.Exceptions.ValidationException;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Self-service account recovery. Forgot/reset are anonymous by design and
/// never reveal whether an email exists; both sit behind a dedicated rate
/// limiter to slow enumeration and token brute-forcing.
/// </summary>
[ApiController]
[Route("api/account")]
public class AccountController : ControllerBase
{
    private readonly IIdentityAccountService _accounts;
    private readonly UserManager<ApplicationUser> _userManager;

    public AccountController(IIdentityAccountService accounts, UserManager<ApplicationUser> userManager)
    {
        _accounts = accounts;
        _userManager = userManager;
    }

    public record ForgotPasswordRequest([Required, EmailAddress] string Email);

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("account-recovery")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _accounts.SendPasswordResetEmailAsync(request.Email, HttpContext.RequestAborted);
        // Always the same response — no account enumeration.
        return Ok(new { message = "If the email is registered, a reset link has been sent." });
    }

    public record ResetPasswordRequest(
        [Required, EmailAddress] string Email,
        [Required] string Token,
        [Required, MinLength(8)] string NewPassword);

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("account-recovery")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            // Same shape as an invalid token: don't leak which emails exist.
            throw new ValidationException("token", "The reset link is invalid or has expired.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var passwordErrors = result.Errors
                .Where(e => e.Code.StartsWith("Password", StringComparison.Ordinal))
                .Select(e => e.Description)
                .ToArray();
            throw passwordErrors.Length > 0
                ? new ValidationException("newPassword", string.Join(" ", passwordErrors))
                : new ValidationException("token", "The reset link is invalid or has expired.");
        }

        // A successful reset proves account ownership — clear any lockout.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        return Ok(new { message = "Password updated. You can sign in now." });
    }

    public record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required, MinLength(8)] string NewPassword);

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(Claims.Subject)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new ForbiddenAccessException("The access token carries no subject.");
        var user = await _userManager.FindByIdAsync(userId)
                   ?? throw new NotFoundException("LoginAccount", userId);

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var mismatch = result.Errors.Any(e => e.Code == "PasswordMismatch");
            throw mismatch
                ? new ValidationException("currentPassword", "The current password is incorrect.")
                : new ValidationException("newPassword", string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return NoContent();
    }
}
