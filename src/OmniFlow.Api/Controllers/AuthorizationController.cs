using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OmniFlow.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers;

// Token issuance must be reachable without a token; OpenIddict enforces
// grant validation itself.
[ApiController]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class AuthorizationController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthorizationController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Handles the OAuth2 token endpoint (Password + Refresh Token grants).
    /// POST /connect/token
    /// </summary>
    [HttpPost("~/connect/token"), IgnoreAntiforgeryToken, Produces("application/json")]
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

        if (user == null)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                }));
        }

        // Validate the password
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password!, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The username/password couple is invalid."
                }));
        }

        var principal = await CreateClaimsPrincipalAsync(user);
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

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
