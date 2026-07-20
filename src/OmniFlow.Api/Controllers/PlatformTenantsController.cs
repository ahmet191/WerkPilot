using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Platform operator surface: stand up new customer workspaces and suspend or
/// restore existing ones. Gated on the tenant-transcending platform role — the
/// permission matrix is per-tenant and cannot express this authority.
/// </summary>
[ApiController]
[Route("api/platform/tenants")]
[Authorize(Policy = PlatformRoles.PlatformAdminPolicy)]
public class PlatformTenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _tenants;

    public PlatformTenantsController(ITenantProvisioningService tenants)
    {
        _tenants = tenants;
    }

    [HttpGet]
    public async Task<IActionResult> List()
        => Ok(await _tenants.ListAsync(HttpContext.RequestAborted));

    public record CreateTenantRequest(
        [Required, MinLength(2)] string Name,
        [Required, MinLength(3)] string Domain,
        [Required, EmailAddress] string AdminEmail,
        [Required, MinLength(2)] string AdminName);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
    {
        var tenantId = await _tenants.ProvisionAsync(
            new ProvisionTenantRequest(request.Name, request.Domain, request.AdminEmail, request.AdminName),
            HttpContext.RequestAborted);

        return Ok(new { tenantId });
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await _tenants.SetActiveAsync(id, true, HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _tenants.SetActiveAsync(id, false, HttpContext.RequestAborted);
        return NoContent();
    }
}
