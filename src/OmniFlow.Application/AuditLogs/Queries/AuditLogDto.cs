namespace OmniFlow.Application.AuditLogs.Queries;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
