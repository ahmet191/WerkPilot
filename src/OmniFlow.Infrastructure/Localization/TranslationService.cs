using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Infrastructure.Localization;

public class TranslationService : ITranslationService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly OmniFlowDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(
        OmniFlowDbContext dbContext,
        ITenantService tenantService,
        IMemoryCache cache,
        ILogger<TranslationService> logger)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetOverridesAsync(string language, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        // Unauthenticated callers (the login page loads strings before sign-in)
        // have no tenant, so there is nothing to override.
        if (tenantId == Guid.Empty)
        {
            return new Dictionary<string, string>();
        }

        var key = CacheKey(tenantId, language);
        if (_cache.TryGetValue(key, out Dictionary<string, string>? cached) && cached != null)
        {
            return cached;
        }

        var overrides = await _dbContext.TranslationOverrides
            .Where(t => t.Language == language)
            .Select(t => new { t.Key, t.Value })
            .ToListAsync(cancellationToken);

        var map = overrides.ToDictionary(o => o.Key, o => o.Value, StringComparer.Ordinal);
        _cache.Set(key, map, CacheLifetime);
        return map;
    }

    public async Task SetAsync(string language, string key, string value, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        // Reverting soft-deletes the row, but it keeps occupying the
        // (tenant, language, key) unique index — so customizing the same key
        // again must resurrect that row rather than insert a duplicate.
        var existing = await _dbContext.TranslationOverrides
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Language == language && t.Key == key, cancellationToken);

        if (existing is null)
        {
            _dbContext.TranslationOverrides.Add(new TranslationOverride
            {
                TenantId = tenantId,
                Language = language,
                Key = key,
                Value = value
            });
        }
        else
        {
            existing.Value = value;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        Invalidate(language);
        _logger.LogInformation("Translation '{Key}' ({Language}) customized for tenant {TenantId}.",
            key, language, _tenantService.TenantId);
    }

    public async Task RemoveAsync(string language, string key, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.TranslationOverrides
            .FirstOrDefaultAsync(t => t.Language == language && t.Key == key, cancellationToken);
        if (existing is null) return;

        _dbContext.TranslationOverrides.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);
        Invalidate(language);
    }

    private void Invalidate(string language) => _cache.Remove(CacheKey(_tenantService.TenantId, language));

    private static string CacheKey(Guid tenantId, string language) => $"translations:{tenantId}:{language}";
}
