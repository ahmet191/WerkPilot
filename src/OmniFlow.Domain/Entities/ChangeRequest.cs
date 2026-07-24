using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class ChangeRequest : AuditableEntity
{
    public string ChangeNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ChangeType Type { get; set; }
    public ChangeRisk RiskLevel { get; set; }
    public ChangeState State { get; set; }
    
    public string? ImplementationPlan { get; set; }
    public string? BackoutPlan { get; set; }
    public string? TestPlan { get; set; }
    
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    
    public Guid? RequesterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid ServiceDeskId { get; set; }
    
    // Navigation
    public User? Requester { get; set; }
    public User? Assignee { get; set; }
    public ServiceDesk? ServiceDesk { get; set; }
    
    // CIs affected by this change
    public ICollection<ConfigurationItem> ConfigurationItems { get; set; } = new List<ConfigurationItem>();
}
