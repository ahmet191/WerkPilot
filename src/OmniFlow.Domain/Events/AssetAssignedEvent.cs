namespace OmniFlow.Domain.Events;

public class AssetAssignedEvent
{
    /// <summary>Consumers run outside any HTTP context — they scope the DbContext with this.</summary>
    public Guid TenantId { get; set; }

    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public Guid AssignedToUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
