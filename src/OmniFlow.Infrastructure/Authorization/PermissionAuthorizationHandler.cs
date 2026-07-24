using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Authorization;

/// <summary>
/// Answers "may this caller do X?" by resolving their role claims against the
/// current tenant's matrix. Roles arrive in the access token; the matrix lives
/// in the database, so a grant or revocation applies on the next request.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string RoleClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private readonly IPermissionService _permissions;
    private readonly ITenantService _tenantService;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionService permissions,
        ITenantService tenantService,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissions = permissions;
        _tenantService = tenantService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var roles = ExtractRoles(context.User);
        if (roles.Count == 0)
        {
            return;
        }

        var tenantId = _tenantService.TenantId;
        if (await _permissions.HasPermissionAsync(tenantId, roles, requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        _logger.LogInformation(
            "Permission {Permission} denied for roles [{Roles}] in tenant {TenantId}.",
            requirement.Permission, string.Join(", ", roles), tenantId);
    }

    /// <summary>Role claims appear under the bare name or the SOAP URI depending on the token pipeline.</summary>
    private static List<string> ExtractRoles(ClaimsPrincipal user) =>
        user.Claims
            .Where(c => c.Type is ClaimTypes.Role or "role" or RoleClaimUri)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
}
