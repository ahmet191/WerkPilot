namespace OmniFlow.Domain.Events;

public class LeaveRequestCreatedEvent
{
    public Guid LeaveRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class LeaveRequestResolvedEvent
{
    public Guid LeaveRequestId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid ApproverId { get; set; }
    public bool IsApproved { get; set; }
}
