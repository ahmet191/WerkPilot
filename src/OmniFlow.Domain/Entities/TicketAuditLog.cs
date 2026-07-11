namespace OmniFlow.Domain.Entities;

public class TicketAuditLog
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid ChangedById { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string Action { get; set; } = string.Empty; // e.g. "Create", "Transition", "Assign", "FieldUpdate"
    
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public User ChangedBy { get; set; } = null!;
}
