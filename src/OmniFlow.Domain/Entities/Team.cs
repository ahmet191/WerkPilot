using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class Team : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? TeamLeadId { get; set; }
    
    // Navigation
    public Department Department { get; set; } = null!;
    public User? TeamLead { get; set; }
}
