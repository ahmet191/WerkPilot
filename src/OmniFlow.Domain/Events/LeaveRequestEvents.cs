namespace OmniFlow.Domain.Events;

// Integration events travel through the MassTransit outbox and are consumed
// outside any HTTP context: TenantId MUST be carried on the contract so
// consumers can scope their DbContext to the right tenant.

public class LeaveRequestCreatedEvent
{
    public Guid TenantId { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class LeaveRequestResolvedEvent
{
    public Guid TenantId { get; set; }
    public Guid LeaveRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ApproverId { get; set; }
    public bool IsApproved { get; set; }
}
