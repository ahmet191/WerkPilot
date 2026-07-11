using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class EmploymentDetails : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTime DateOfHire { get; set; }
    public DateTime? DateOfTermination { get; set; }
    public string ContractType { get; set; } = string.Empty; // e.g., Full-Time, Part-Time, Contractor
    public EmploymentStatus Status { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
