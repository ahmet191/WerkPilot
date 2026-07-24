using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Corporate SSO endpoints. The browser is sent through the external OIDC
/// provider; the callback maps the asserted identity to a local account and
/// hands the SPA a one-time ticket it swaps for tokens at /connect/token
/// (grant_type=urn:werkpilot:sso). The ticket travels in the URL fragment so
/// it never hits server logs.
/// </summary>
[ApiController]
[Route("api/auth/sso")]
[AllowAnonymous]
public class SsoController : ControllerBase
{
    public const string OidcScheme = "ExternalOidc";
    public const string ExternalCookieScheme = "ExternalCookie";

    private readonly IConfiguration _configuration;
    private readonly ExternalUserProvisioner _provisioner;
    private readonly SsoTicketService _tickets;
    private readonly ILogger<SsoController> _logger;

    public SsoController(
        IConfiguration configuration,
        ExternalUserProvisioner provisioner,
        SsoTicketService tickets,
        ILogger<SsoController> logger)
    {
        _configuration = configuration;
        _provisioner = provisioner;
        _tickets = tickets;
        _logger = logger;
    }

    private bool OidcEnabled =>
        _configuration.GetValue<bool>("Sso:Oidc:Enabled")
        && !string.IsNullOrWhiteSpace(_configuration["Sso:Oidc:Authority"]);

    private string SpaCallbackUrl =>
        _configuration["Sso:SpaCallbackUrl"] ?? "http://localhost:5173/login/sso";

    /// <summary>Login page asks which SSO buttons to render.</summary>
    [HttpGet("providers")]
    public IActionResult Providers()
    {
        var providers = new List<object>();
        if (OidcEnabled)
        {
            providers.Add(new
            {
                id = "oidc",
                displayName = _configuration["Sso:Oidc:DisplayName"] ?? "Corporate SSO"
            });
        }
        return Ok(providers);
    }

    /// <summary>Kicks off the OIDC dance with the corporate IdP.</summary>
    [HttpGet("challenge")]
    public IActionResult ChallengeProvider()
    {
        if (!OidcEnabled)
        {
            return NotFound();
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = "/api/auth/sso/callback" },
            OidcScheme);
    }

    /// <summary>IdP round-trip landed: map the identity, mint the SPA ticket.</summary>
    [HttpGet("callback")]
    public async Task<IActionResult> Callback()
    {
        var result = await HttpContext.AuthenticateAsync(ExternalCookieScheme);
        if (!result.Succeeded || result.Principal == null)
        {
            _logger.LogWarning("SSO callback reached without a valid external session.");
            return Redirect($"{SpaCallbackUrl}#error=auth_failed");
        }

        var principal = result.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email")
                    ?? principal.FindFirstValue("preferred_username");
        var displayName = principal.FindFirstValue(ClaimTypes.Name)
                          ?? principal.FindFirstValue("name");

        await HttpContext.SignOutAsync(ExternalCookieScheme);

        if (string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("SSO identity carried no email claim; cannot map to a local account.");
            return Redirect($"{SpaCallbackUrl}#error=no_email");
        }

        var user = await _provisioner.FindOrProvisionAsync(
            email,
            displayName,
            "oidc",
            _configuration.GetValue<bool>("Sso:Oidc:AutoProvision"),
            _configuration["Sso:Oidc:DefaultRole"] ?? "Requester",
            HttpContext.RequestAborted);

        if (user == null)
        {
            return Redirect($"{SpaCallbackUrl}#error=user_not_found");
        }

        var ticket = await _tickets.IssueAsync(user.Id, "oidc", HttpContext.RequestAborted);
        return Redirect($"{SpaCallbackUrl}#ticket={ticket}");
    }
}
