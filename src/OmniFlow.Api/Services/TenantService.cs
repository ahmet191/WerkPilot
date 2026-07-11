using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class TenantService : ITenantService
{
    private Guid _tenantId;

    public TenantService()
    {
        // For demonstration, default to a dummy tenant ID
        _tenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    }

    public Guid TenantId => _tenantId;

    public void SetTenantId(Guid tenantId)
    {
        _tenantId = tenantId;
    }
}
