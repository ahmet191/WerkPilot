using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class SLAConfiguration : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    
    // The target priority (e.g., "High", "Critical")
    public string PriorityLevel { get; set; } = string.Empty;
    
    // SLA duration in minutes
    public int ResolutionTimeMinutes { get; set; }
    public int FirstResponseTimeMinutes { get; set; }
    
    // Navigation
    public Department Department { get; set; } = null!;
}
