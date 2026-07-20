namespace OmniFlow.Application.Interfaces;

public record TenantSummary(
    Guid Id,
    string Name,
    string Domain,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount,
    int TicketCount);

public record ProvisionTenantRequest(
    string Name,
    string Domain,
    string AdminEmail,
    string AdminName);

/// <summary>
/// Platform-level tenant lifecycle: stands up a new customer workspace
/// (baseline data + its first administrator) and toggles tenant access.
/// Operates across tenants, so it is gated by the platform role rather than
/// the per-tenant permission matrix.
/// </summary>
public interface ITenantProvisioningService
{
    Task<IReadOnlyList<TenantSummary>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the tenant, seeds its baseline workspace and its first
    /// administrator, then emails that admin an invitation to set a password.
    /// </summary>
    Task<Guid> ProvisionAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Suspends or restores a tenant; suspended tenants cannot sign in.</summary>
    Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken cancellationToken = default);
}
