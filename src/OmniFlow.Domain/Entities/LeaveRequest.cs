using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class LeaveRequest : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public LeaveStatus Status { get; set; }
    public Guid? ApproverId { get; set; }
    public string? Reason { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
    public User? Approver { get; set; }
}
