namespace OmniFlow.Application.Interfaces;

public interface ITenantService
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}
