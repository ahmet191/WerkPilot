using System.ComponentModel.DataAnnotations.Schema;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// A persistent record of a system event (e.g. error, warning, request trace).
/// This entity does NOT inherit from AuditableEntity to avoid recursive logging
/// and to keep technical logs completely separated from domain audit logs.
/// </summary>
public class SystemLog
{
    public Guid Id { get; set; }

    /// <summary>The rendered message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The raw Serilog message template.</summary>
    public string MessageTemplate { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    public string? Exception { get; set; }

    /// <summary>Serilog properties serialized as JSONB.</summary>
    public string Properties { get; set; } = "{}";

    /// <summary>The Trace ID injected by CorrelationIdMiddleware or OpenTelemetry.</summary>
    public string? TraceId { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? UserId { get; set; }
}
