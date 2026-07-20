using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Infrastructure.Identity;
using OmniFlow.Infrastructure.Identity.Ldap;
using OmniFlow.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers;

// Token issuance must be reachable without a token; OpenIddict enforces
// grant validation itself.
[ApiController]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class AuthorizationController : ControllerBase
{
    /// <summary>Custom grant redeeming a one-time SSO exchange ticket.</summary>
    public const string SsoGrantType = "urn:werkpilot:sso";

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILdapAuthenticator _ldapAuthenticator;
    private readonly ExternalUserProvisioner _provisioner;
    private readonly SsoTicketService _ssoTickets;
    private readonly OmniFlowDbContext _dbContext;

    public AuthorizationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILdapAuthenticator ldapAuthenticator,
        ExternalUserProvisioner provisioner,
        SsoTicketService ssoTickets,
        OmniFlowDbContext dbContext)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _ldapAuthenticator = ldapAuthenticator;
        _provisioner = provisioner;
        _ssoTickets = ssoTickets;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Handles the OAuth2 token endpoint (Password + Refresh Token grants).
    /// POST /connect/token
    /// Rate limited per client IP to slow credential brute-forcing.
    /// </summary>
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("token-endpoint")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsPasswordGrantType())
        {
            return await HandlePasswordGrant(request);
        }

        if (request.IsRefreshTokenGrantType())
        {
            return await HandleRefreshTokenGrant();
        }

        if (request.GrantType == SsoGrantType)
        {
            return await HandleSsoTicketGrant(request);
        }

        return Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified grant type is not supported."
            }));
    }

    /// <summary>
    /// Returns the current user's profile info.
    /// GET /connect/userinfo
    /// </summary>
    [HttpGet("~/connect/userinfo"), Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.FindByIdAsync(
            User.FindFirstValue(Claims.Subject) ?? string.Empty);

        if (user == null)
        {
            return Challenge(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidToken,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified access token is bound to an account that no longer exists."
                }));
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id.ToString()
        };

        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email ?? string.Empty;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        if (User.HasScope(Scopes.Profile))
        {
            claims[Claims.Name] = user.UserName ?? string.Empty;
        }

        if (User.HasScope(Scopes.Roles))
        {
            claims[Claims.Role] = await _userManager.GetRolesAsync(user);
        }

        // Include tenant_id custom claim
        claims["tenant_id"] = user.TenantId.ToString();

        return Ok(claims);
    }

    // ── Private helpers ──

    private async Task<IActionResult> HandlePasswordGrant(OpenIddictRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.Username!)
                   ?? await _userManager.FindByEmailAsync(request.Username!);

        var credentialsValid = false;

        if (user != null && await _userManager.HasPasswordAsync(user))
        {
            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                return TokenError(Errors.InvalidGrant, "The account is temporarily locked. Try again later.");
            }
            credentialsValid = result.Succeeded;
        }

        // Optional directory fallback: local password auth failed (or the user
        // has no local password), so let the corporate LDAP verify the
        // credentials and map/provision the local account.
        if (!credentialsValid && _ldapAuthenticator.Enabled)
        {
            var directoryUser = await _ldapAuthenticator.ValidateAsync(request.Username!, request.Password!, HttpContext.RequestAborted);
            if (directoryUser != null)
            {
                var ldapOptions = _ldapAuthenticator.Options;
                var email = directoryUser.Email ?? $"{directoryUser.Username}@{ldapOptions.FallbackEmailDomain}";
                user = await _provisioner.FindOrProvisionAsync(
                    email, directoryUser.DisplayName, "ldap",
                    ldapOptions.AutoProvision, ldapOptions.DefaultRole, HttpContext.RequestAborted);
                if (user == null)
                {
                    return TokenError(Errors.InvalidGrant, "The directory account has no matching local account.");
                }
                credentialsValid = true;
            }
        }

        if (user == null || !credentialsValid)
        {
            return TokenError(Errors.InvalidGrant, "The username/password couple is invalid.");
        }

        if (!await IsTenantActiveAsync(user))
        {
            return TokenError(Errors.InvalidGrant, "This workspace is suspended. Contact your administrator.");
        }

        // MFA step-up: TOTP (or a recovery code) is required once enabled.
        if (user.TwoFactorEnabled)
        {
            var rawCode = ((string?)request.GetParameter("mfa_code"))?.Trim();
            if (string.IsNullOrWhiteSpace(rawCode))
            {
                return TokenError("mfa_required", "A multi-factor authentication code is required.");
            }

            // TOTP input tolerates cosmetic grouping; recovery codes must keep
            // their canonical xxxxx-xxxxx shape (Identity compares literally).
            var totpCode = rawCode.Replace(" ", string.Empty).Replace("-", string.Empty);
            var totpValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, totpCode);

            if (!totpValid)
            {
                var recovery = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, rawCode);
                if (!recovery.Succeeded)
                {
                    // Failed second factors feed the same lockout as passwords.
                    await _userManager.AccessFailedAsync(user);
                    return TokenError(Errors.InvalidGrant, "The multi-factor authentication code is invalid.");
                }
            }
        }

        var principal = await CreateClaimsPrincipalAsync(user);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<IActionResult> HandleSsoTicketGrant(OpenIddictRequest request)
    {
        var ticket = (string?)request.GetParameter("ticket");
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return TokenError(Errors.InvalidRequest, "The 'ticket' parameter is required.");
        }

        var userId = await _ssoTickets.RedeemAsync(ticket, HttpContext.RequestAborted);
        if (userId == null)
        {
            return TokenError(Errors.InvalidGrant, "The SSO ticket is invalid, expired, or already used.");
        }

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null || !await _signInManager.CanSignInAsync(user))
        {
            return TokenError(Errors.InvalidGrant, "The user is not allowed to sign in.");
        }

        // MFA is intentionally not re-challenged here: the corporate IdP that
        // produced the ticket is responsible for its own factor policy.
        var principal = await CreateClaimsPrincipalAsync(user);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// A suspended tenant must not be able to obtain tokens — checked on every
    /// grant, so deactivating a workspace takes effect immediately.
    /// </summary>
    private async Task<bool> IsTenantActiveAsync(ApplicationUser user)
    {
        if (user.TenantId == Guid.Empty) return true;
        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, HttpContext.RequestAborted);
        return tenant is null || tenant.IsActive;
    }

    private IActionResult TokenError(string error, string description)
        => Forbid(
            authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }));

    private async Task<IActionResult> HandleRefreshTokenGrant()
    {
        // Retrieve the claims principal stored in the refresh token
        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

        var userId = result.Principal?.FindFirstValue(Claims.Subject);
        var user = userId != null ? await _userManager.FindByIdAsync(userId) : null;

        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                }));
        }

        // Ensure the user is still allowed to sign in
        if (!await _signInManager.CanSignInAsync(user))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is no longer allowed to sign in."
                }));
        }

        var principal = await CreateClaimsPrincipalAsync(user);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreateClaimsPrincipalAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        // Add core claims
        identity.SetClaim(Claims.Subject, user.Id.ToString());
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.UserName);
        identity.SetClaim("tenant_id", user.TenantId.ToString());

        // Add role claims
        var roles = await _userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, roles.ToImmutableArray());

        var principal = new ClaimsPrincipal(identity);

        // Set the scopes the token will include
        principal.SetScopes(new[]
        {
            Scopes.OpenId,
            Scopes.Email,
            Scopes.Profile,
            Scopes.Roles,
            Scopes.OfflineAccess
        });

        // Set claim destinations (which claims go to access vs identity tokens)
        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name or Claims.Email
                => new[] { Destinations.AccessToken, Destinations.IdentityToken },

            Claims.Role
                => new[] { Destinations.AccessToken, Destinations.IdentityToken },

            "tenant_id"
                => new[] { Destinations.AccessToken, Destinations.IdentityToken },

            _ => new[] { Destinations.AccessToken }
        });

        return principal;
    }
}
