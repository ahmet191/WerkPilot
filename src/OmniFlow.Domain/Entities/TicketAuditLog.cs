using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// Ticket change history. Inherits AuditableEntity so rows are tenant-scoped
/// like every other record.
/// </summary>
public class TicketAuditLog : AuditableEntity
{
    public Guid TicketId { get; set; }
    public Guid ChangedById { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string Action { get; set; } = string.Empty; // e.g. "Create", "Transition", "Assign", "FieldUpdate"

    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }

    public User ChangedBy { get; set; } = null!;
}
