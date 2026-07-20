using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// One grant in a tenant's permission matrix: role X may do Y. Tenant-scoped
/// through <see cref="AuditableEntity"/>, so two tenants can shape the same
/// role name differently.
/// </summary>
public class RolePermission : AuditableEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string Permission { get; set; } = string.Empty;
}
