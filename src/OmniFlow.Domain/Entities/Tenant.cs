using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// Tenant registry root. Deliberately NOT an AuditableEntity: it must stay
/// outside the tenant global query filter, since it is the record the filter
/// resolves against.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty; // e.g., acmecorp.omniflow.com
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Tenant-wide default approval chain for leave requests (JSONB).
    /// Seeded as manager → HRAdmin; editable per tenant.
    /// </summary>
    public ApprovalDefinition? LeaveApprovalPolicy { get; set; }
}
