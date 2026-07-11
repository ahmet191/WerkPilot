using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class TicketTemplate : AuditableEntity
{
    public Guid Id { get; set; }
    
    // Multi-lingual name (e.g., {"en": "IT Support", "tr": "BT Destek"})
    public MultiLingualString LocalizedName { get; set; } = new();
    
    public Guid ServiceDeskId { get; set; }
    public ServiceDesk? ServiceDesk { get; set; }
    public List<FieldDefinition> Fields { get; set; } = new();
    
    // Dynamic workflow transitions configured by admins
    public WorkflowDefinition Workflow { get; set; } = new();
}

public class FieldDefinition
{
    // The technical key used in the JSON payload (e.g., "asset_tag")
    public string Key { get; set; } = string.Empty;
    
    public MultiLingualString LocalizedName { get; set; } = new();
    public string Type { get; set; } = string.Empty; // e.g. "Text", "Number", "Date", "Dropdown"
    public bool IsRequired { get; set; }
    public string? ValidationRegex { get; set; }
    public List<string>? DropdownOptions { get; set; }
    public MultiLingualString LocalizedDescription { get; set; } = new();
}

public class WorkflowDefinition
{
    public List<WorkflowTransition> Transitions { get; set; } = new();
}

public class WorkflowTransition
{
    public TicketState FromState { get; set; }
    public TicketState ToState { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public UserRole? RequiredRole { get; set; } // Enforce role-based state updates
}
