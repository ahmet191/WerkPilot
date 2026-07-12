using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities.Approvals;

/// <summary>
/// Names of entities that can be governed by an approval chain. Kept as
/// strings so new modules can plug in without schema changes.
/// </summary>
public static class ApprovalEntityTypes
{
    public const string Ticket = "Ticket";
    public const string LeaveRequest = "LeaveRequest";
}

/// <summary>
/// A sequential approval chain attached to a business entity. Steps are
/// decided one at a time (CurrentStepOrder); the first rejection rejects the
/// whole request, approving the final step approves it.
/// </summary>
public class ApprovalRequest : AuditableEntity
{
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }

    /// <summary>Short human-readable context (ticket title, leave summary).</summary>
    public string EntityDisplay { get; set; } = string.Empty;

    public Guid RequestedById { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public int CurrentStepOrder { get; set; } = 1;
    public DateTimeOffset? CompletedAt { get; set; }

    public List<ApprovalStep> Steps { get; set; } = new();
}

public class ApprovalStep : AuditableEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = null!;

    /// <summary>1-based position in the chain.</summary>
    public int Order { get; set; }

    public ApproverType ApproverType { get; set; }

    /// <summary>Identity role allowed to decide, when ApproverType is Role.</summary>
    public string? ApproverRole { get; set; }

    /// <summary>Concrete approver (SpecificUser, or resolved manager).</summary>
    public Guid? ApproverUserId { get; set; }

    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public Guid? ActedById { get; set; }
    public DateTimeOffset? ActedAt { get; set; }
    public string? Comment { get; set; }
}
