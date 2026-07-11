using System;

namespace OmniFlow.Domain.Events;

public class AssetAssignedEvent
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public Guid AssignedToUserId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
