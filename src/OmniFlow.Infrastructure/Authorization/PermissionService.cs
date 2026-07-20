using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Infrastructure.Authorization;

public class PermissionService : IPermissionService
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private readonly OmniFlowDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(OmniFlowDbContext dbContext, IMemoryCache cache, ILogger<PermissionService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid tenantId, IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var roleList = roles as IReadOnlyCollection<string> ?? roles.ToList();

        // The super role is deliberately not matrix-driven: no configuration
        // mistake may ever strip an Administrator of access.
        if (roleList.Contains(Permissions.SuperRole, StringComparer.Ordinal))
        {
            return Permissions.All.Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
        }

        var matrix = await GetCachedMatrixAsync(tenantId, cancellationToken);
        var effective = new HashSet<string>(StringComparer.Ordinal);
        foreach (var role in roleList)
        {
            if (matrix.TryGetValue(role, out var granted))
            {
                effective.UnionWith(granted);
            }
        }
        return effective;
    }

    public async Task<bool> HasPermissionAsync(Guid tenantId, IEnumerable<string> roles, string permission, CancellationToken cancellationToken = default)
    {
        var effective = await GetEffectivePermissionsAsync(tenantId, roles, cancellationToken);
        return effective.Contains(permission);
    }

    public async Task<IReadOnlyDictionary<string, string[]>> GetMatrixAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var matrix = await GetCachedMatrixAsync(tenantId, cancellationToken);
        return matrix.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray(), StringComparer.Ordinal);
    }

    public async Task SetRolePermissionsAsync(Guid tenantId, string roleName, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default)
    {
        if (string.Equals(roleName, Permissions.SuperRole, StringComparison.Ordinal))
        {
            throw new ValidationException("roleName", $"'{Permissions.SuperRole}' always holds every permission and cannot be edited.");
        }

        var unknown = permissions.Where(p => !Permissions.IsKnown(p)).ToList();
        if (unknown.Count > 0)
        {
            throw new ValidationException("permissions", $"Unknown permission(s): {string.Join(", ", unknown)}.");
        }

        // Cross-tenant write path: the caller supplies the tenant explicitly, so
        // bypass the query filter and scope by hand.
        var existing = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && p.RoleName == roleName && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var desired = permissions.Distinct(StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);

        foreach (var row in existing.Where(r => !desired.Contains(r.Permission)))
        {
            _dbContext.RolePermissions.Remove(row);
        }

        var current = existing.Select(r => r.Permission).ToHashSet(StringComparer.Ordinal);
        foreach (var permission in desired.Where(p => !current.Contains(p)))
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                TenantId = tenantId,
                RoleName = roleName,
                Permission = permission
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _cache.Remove(CacheKey(tenantId));
        _logger.LogInformation("Permission matrix updated for role {Role} in tenant {TenantId} ({Count} grants).",
            roleName, tenantId, desired.Count);
    }

    private async Task<Dictionary<string, HashSet<string>>> GetCachedMatrixAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey(tenantId), out Dictionary<string, HashSet<string>>? cached) && cached != null)
        {
            return cached;
        }

        var rows = await _dbContext.RolePermissions
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .Select(p => new { p.RoleName, p.Permission })
            .ToListAsync(cancellationToken);

        var matrix = rows
            .GroupBy(r => r.RoleName, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => r.Permission).ToHashSet(StringComparer.Ordinal),
                StringComparer.Ordinal);

        _cache.Set(CacheKey(tenantId), matrix, CacheLifetime);
        return matrix;
    }

    private static string CacheKey(Guid tenantId) => $"permission-matrix:{tenantId}";
}
