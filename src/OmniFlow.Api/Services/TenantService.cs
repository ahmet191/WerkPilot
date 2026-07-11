using Microsoft.AspNetCore.Http;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private Guid? _tenantIdOverride;

    public TenantService(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
    }

    public Guid TenantId
    {
        get
        {
            // Explicit override wins: used by seeding and message consumers,
            // which run outside an HTTP request.
            if (_tenantIdOverride.HasValue)
            {
                return _tenantIdOverride.Value;
            }

            var tenantClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantClaim, out var claimTenantId))
            {
                return claimTenantId;
            }

            // Header resolution is spoofable and only acceptable for local tooling.
            // Disabled unless explicitly opted in via configuration.
            if (_configuration.GetValue<bool>("Multitenancy:AllowHeaderFallback"))
            {
                var tenantHeader = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
                if (Guid.TryParse(tenantHeader, out var headerTenantId))
                {
                    return headerTenantId;
                }
            }

            // Unauthenticated pipeline (e.g. token endpoint): no tenant. The global
            // query filter then matches nothing, which is the safe default.
            return Guid.Empty;
        }
    }

    public void SetTenantId(Guid tenantId)
    {
        _tenantIdOverride = tenantId;
    }
}
