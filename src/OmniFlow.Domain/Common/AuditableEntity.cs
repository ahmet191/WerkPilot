namespace OmniFlow.Domain.Common;

/// <summary>
/// Base for every tenant-owned entity: identity + domain events (BaseEntity),
/// audit stamps, multi-tenancy key, and soft deletion. The global query filter
/// enforces <c>TenantId == currentTenant &amp;&amp; !IsDeleted</c> on all
/// descendants.
/// </summary>
public abstract class AuditableEntity : BaseEntity, ISoftDelete
{
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>Global multi-tenancy key, stamped by the save interceptor.</summary>
    public Guid TenantId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
}
