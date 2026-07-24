using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// Asset aggregate. Assignment drives the status: assigning sets
/// <see cref="AssetStatus.Assigned"/>, unassigning returns it to stock.
/// </summary>
public class Asset : AuditableEntity
{
    private Asset() { }

    public string AssetTag { get; private set; } = string.Empty;
    public string SerialNumber { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;      // e.g., "MacBook Pro 16"
    public string Category { get; private set; } = string.Empty;   // e.g., "Laptop", "Mobile Phone"
    public AssetStatus Status { get; private set; }
    public Guid? AssignedToUserId { get; private set; }

    // Navigation
    public User? AssignedToUser { get; private set; }

    public static Asset Create(string assetTag, string serialNumber, string model, string category, AssetStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            AssetTag = assetTag,
            SerialNumber = serialNumber,
            Model = model,
            Category = category,
            Status = status,
        };

    /// <summary>Updates the descriptive fields (not assignment/status).</summary>
    public void UpdateDetails(string assetTag, string serialNumber, string model, string category)
    {
        AssetTag = assetTag;
        SerialNumber = serialNumber;
        Model = model;
        Category = category;
    }

    /// <summary>Assigns the asset to a user and marks it Assigned.</summary>
    public void AssignTo(Guid userId)
    {
        AssignedToUserId = userId;
        Status = AssetStatus.Assigned;
    }

    /// <summary>Returns the asset to stock.</summary>
    public void Unassign()
    {
        AssignedToUserId = null;
        Status = AssetStatus.InStock;
    }
}
