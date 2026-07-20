using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// The tenant's role → permission matrix. Reading your own effective
/// permissions is open to any signed-in user (the SPA gates its UI with it);
/// editing the matrix requires <c>permissions.manage</c>.
/// </summary>
[ApiController]
[Route("api/permissions")]
public class PermissionsController : ControllerBase
{
    private const string RoleClaimUri = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

    private readonly IPermissionService _permissions;
    private readonly ITenantService _tenantService;

    public PermissionsController(IPermissionService permissions, ITenantService tenantService)
    {
        _permissions = permissions;
        _tenantService = tenantService;
    }

    /// <summary>Every permission the platform knows about, for the matrix editor.</summary>
    [HttpGet("catalog")]
    [Authorize(Policy = PermissionPolicyNames.PermissionsManage)]
    public IActionResult Catalog() => Ok(Permissions.All.Select(p => new
    {
        key = p.Key,
        module = p.Module,
        displayName = p.DisplayName
    }));

    [HttpGet("matrix")]
    [Authorize(Policy = PermissionPolicyNames.PermissionsManage)]
    public async Task<IActionResult> Matrix()
    {
        var matrix = await _permissions.GetMatrixAsync(_tenantService.TenantId, HttpContext.RequestAborted);
        return Ok(new
        {
            superRole = Permissions.SuperRole,
            roles = Permissions.DefaultMatrix.Keys,
            matrix
        });
    }

    public record SetRolePermissionsRequest([Required] List<string> Permissions);

    [HttpPut("matrix/{roleName}")]
    [Authorize(Policy = PermissionPolicyNames.PermissionsManage)]
    public async Task<IActionResult> SetRolePermissions(string roleName, [FromBody] SetRolePermissionsRequest request)
    {
        await _permissions.SetRolePermissionsAsync(
            _tenantService.TenantId, roleName, request.Permissions, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>The caller's own effective permissions — drives UI gating.</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> Mine()
    {
        var roles = User.Claims
            .Where(c => c.Type is ClaimTypes.Role or "role" or RoleClaimUri)
            .Select(c => c.Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var effective = await _permissions.GetEffectivePermissionsAsync(
            _tenantService.TenantId, roles, HttpContext.RequestAborted);

        return Ok(new { roles, permissions = effective.OrderBy(p => p, StringComparer.Ordinal) });
    }
}
