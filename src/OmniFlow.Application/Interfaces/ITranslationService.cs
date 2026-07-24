namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Tenant-specific UI string overrides. The built-in dictionary remains the
/// default; this only stores and serves what a tenant has customized. Reads are
/// cached per (tenant, language) and invalidated on every write, so an edit
/// applies on the next request without a restart.
/// </summary>
public interface ITranslationService
{
    /// <summary>The calling tenant's overrides for a language, keyed by translation key.</summary>
    Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(string language, CancellationToken cancellationToken = default);

    /// <summary>Creates or updates one override.</summary>
    Task SetAsync(string language, string key, string value, CancellationToken cancellationToken = default);

    /// <summary>Removes an override, reverting the key to the shipped default.</summary>
    Task RemoveAsync(string language, string key, CancellationToken cancellationToken = default);
}
