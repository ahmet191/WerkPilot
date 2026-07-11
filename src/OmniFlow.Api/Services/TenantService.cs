using Microsoft.AspNetCore.Http;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantIdOverride;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            if (_tenantIdOverride.HasValue)
            {
                return _tenantIdOverride.Value;
            }

            // Try to get from JWT claims
            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out var claimTenantId))
            {
                return claimTenantId;
            }

            // Fallback to headers for legacy
            var tenantHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (Guid.TryParse(tenantHeader, out var headerTenantId))
            {
                return headerTenantId;
            }

            // Return a dummy/default tenant ID for testing if not found
            return Guid.Empty;
        }
    }

    public void SetTenantId(Guid tenantId)
    {
        _tenantIdOverride = tenantId;
    }
}
