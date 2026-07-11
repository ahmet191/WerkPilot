using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Asset : AuditableEntity
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty; // e.g., "MacBook Pro 16"
    public string Category { get; set; } = string.Empty; // e.g., "Laptop", "Mobile Phone"
    public AssetStatus Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    
    // Navigation
    public User? AssignedToUser { get; set; }
}
