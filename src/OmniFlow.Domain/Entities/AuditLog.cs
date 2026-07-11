using System;

namespace OmniFlow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty; // e.g., POST, PUT, DELETE
    public string EntityType { get; set; } = string.Empty; // e.g., /api/tickets
    public string? EntityId { get; set; } // Can be parsed from URL if possible
    public string? Details { get; set; } // JSON payload or description
    public DateTime Timestamp { get; set; }
}
