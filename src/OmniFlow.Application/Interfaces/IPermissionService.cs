namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Resolves and edits a tenant's role → permission matrix. Reads are cached per
/// tenant and invalidated on every write, so runtime grants take effect on the
/// caller's next request without a restart.
/// </summary>
public interface IPermissionService
{
    /// <summary>Effective permissions for a set of role names in the given tenant.</summary>
    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(Guid tenantId, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    Task<bool> HasPermissionAsync(Guid tenantId, IEnumerable<string> roles, string permission, CancellationToken cancellationToken = default);

    /// <summary>The editable matrix (role → permissions) for the tenant, excluding the super role.</summary>
    Task<IReadOnlyDictionary<string, string[]>> GetMatrixAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Replaces one role's grants wholesale. Unknown permission keys are rejected.</summary>
    Task SetRolePermissionsAsync(Guid tenantId, string roleName, IReadOnlyCollection<string> permissions, CancellationToken cancellationToken = default);
}
