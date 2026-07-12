using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// HTTP-level audit trail written by the audit middleware. Inherits
/// AuditableEntity so rows are tenant-scoped like every other record.
/// </summary>
public class AuditLog : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., POST, PUT, DELETE
    public string EntityType { get; set; } = string.Empty; // e.g., /api/tickets
    public string? EntityId { get; set; } // Can be parsed from URL if possible
    public string? Details { get; set; } // JSON payload or description
    public DateTime Timestamp { get; set; }
}
