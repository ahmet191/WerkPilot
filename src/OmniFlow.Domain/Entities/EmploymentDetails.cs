using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class EmploymentDetails : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime DateOfHire { get; set; }
    public DateTime? DateOfTermination { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.FullTime;
    public EmploymentStatus Status { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
