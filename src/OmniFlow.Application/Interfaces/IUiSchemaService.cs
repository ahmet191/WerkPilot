namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Tenant-specific UI schema overrides. The built-in schemas remain the default;
/// this only stores and serves what a tenant has customized. Reads are cached
/// per tenant and invalidated on every write, so an edit applies on the next
/// request without a restart.
/// </summary>
public interface IUiSchemaService
{
    /// <summary>The calling tenant's override JSON for an entity type, or null if none.</summary>
    Task<string?> GetOverrideAsync(string entityType, CancellationToken cancellationToken = default);

    /// <summary>The entity types the calling tenant has customized.</summary>
    Task<IReadOnlyCollection<string>> GetCustomizedEntityTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>Creates or updates one entity's schema override.</summary>
    Task SetAsync(string entityType, string schemaJson, CancellationToken cancellationToken = default);

    /// <summary>Removes an override, reverting the entity to the shipped schema.</summary>
    Task RemoveAsync(string entityType, CancellationToken cancellationToken = default);
}
