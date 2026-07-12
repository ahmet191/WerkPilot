namespace OmniFlow.Domain.Events;

/// <summary>
/// Raised when an approval request is created or advances to a new step, so
/// the (next) approver can be notified. Carries TenantId for consumer scoping.
/// </summary>
public class ApprovalStepActivatedEvent
{
    public Guid TenantId { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityDisplay { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string? ApproverRole { get; set; }
}

/// <summary>
/// Raised when the whole approval chain concludes. Consumers apply the
/// outcome to the governed entity (ticket transition, leave status).
/// </summary>
public class ApprovalCompletedEvent
{
    public Guid TenantId { get; set; }
    public Guid ApprovalRequestId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public bool IsApproved { get; set; }
    public Guid DecidedById { get; set; }
    public string? Comment { get; set; }
}
