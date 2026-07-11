using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Ticket : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public Guid RequesterId { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? CategoryId { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketState CurrentState { get; set; }
    
    // ITSM Fields
    public TicketType Type { get; set; } = TicketType.Incident;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketImpact Impact { get; set; } = TicketImpact.Medium;
    public TicketUrgency Urgency { get; set; } = TicketUrgency.Medium;
    
    public string? ResolutionNotes { get; set; }
    public List<string> Tags { get; set; } = new();
    public Guid? ServiceDeskId { get; set; }
    public ServiceDesk? ServiceDesk { get; set; }
    
    // CMDB & Problem Management References
    public Guid? ConfigurationItemId { get; set; }
    public Guid? ProblemId { get; set; }
    
    // Navigation
    public ConfigurationItem? ConfigurationItem { get; set; }
    public Problem? Problem { get; set; }
    
    // SLAs
    public DateTimeOffset? SlaDueDate { get; set; }
    public DateTimeOffset? ResolutionDate { get; set; }
    
    // Dynamic properties mapped via JSON conversion
    public Dictionary<string, object> DynamicData { get; set; } = new();

    public TicketTemplate Template { get; set; } = null!;
    public User Requester { get; set; } = null!;
    public User? Assignee { get; set; }
    public TicketCategory? Category { get; set; }
    
    public List<TicketComment> Comments { get; set; } = new();
    public List<TicketAuditLog> AuditLogs { get; set; } = new();
}
