using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class LeaveBalance : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public LeaveType Type { get; set; }
    public decimal TotalDaysAllocated { get; set; }
    public decimal TotalDaysUsed { get; set; }
    public int Year { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
