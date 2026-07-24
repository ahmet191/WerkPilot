using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Infrastructure.UiSchemas;

public class UiSchemaService : IUiSchemaService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly OmniFlowDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UiSchemaService> _logger;

    public UiSchemaService(
        OmniFlowDbContext dbContext,
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<UiSchemaService> logger)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> GetOverrideAsync(string entityType, CancellationToken cancellationToken = default)
    {
        var map = await LoadAsync(cancellationToken);
        return map.GetValueOrDefault(entityType.ToLowerInvariant());
    }

    public async Task<IReadOnlyCollection<string>> GetCustomizedEntityTypesAsync(CancellationToken cancellationToken = default)
    {
        var map = await LoadAsync(cancellationToken);
        return map.Keys.ToArray();
    }

    private async Task<Dictionary<string, string>> LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.TenantId;
        if (tenantId == Guid.Empty)
        {
            return new Dictionary<string, string>();
        }

        var key = CacheKey(tenantId);
        if (_cache.TryGetValue(key, out Dictionary<string, string>? cached) && cached != null)
        {
            return cached;
        }

        var overrides = await _dbContext.UiSchemaOverrides
            .Select(o => new { o.EntityType, o.SchemaJson })
            .ToListAsync(cancellationToken);

        var map = overrides.ToDictionary(o => o.EntityType.ToLowerInvariant(), o => o.SchemaJson, StringComparer.Ordinal);
        _cache.Set(key, map, CacheLifetime);
        return map;
    }

    public async Task SetAsync(string entityType, string schemaJson, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;
        entityType = entityType.ToLowerInvariant();

        // Reverting soft-deletes the row, which still occupies the
        // (tenant, entityType) unique index — so re-customizing must resurrect it.
        var existing = await _dbContext.UiSchemaOverrides
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.EntityType == entityType, cancellationToken);

        if (existing is null)
        {
            _dbContext.UiSchemaOverrides.Add(new UiSchemaOverride
            {
                TenantId = tenantId,
                EntityType = entityType,
                SchemaJson = schemaJson
            });
        }
        else
        {
            existing.SchemaJson = schemaJson;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        Invalidate();
        _logger.LogInformation("UI schema for '{EntityType}' customized for tenant {TenantId}.", entityType, tenantId);
    }

    public async Task RemoveAsync(string entityType, CancellationToken cancellationToken = default)
    {
        entityType = entityType.ToLowerInvariant();
        var existing = await _dbContext.UiSchemaOverrides
            .FirstOrDefaultAsync(o => o.EntityType == entityType, cancellationToken);
        if (existing is null) return;

        _dbContext.UiSchemaOverrides.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        Invalidate();
    }

    private void Invalidate() => _cache.Remove(CacheKey(_tenantService.TenantId));

    private static string CacheKey(Guid tenantId) => $"uischema:{tenantId}";
}
