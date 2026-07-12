using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class ConfigurationItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CIType Type { get; set; }
    public CIStatus Status { get; set; }
    public string? SerialNumber { get; set; }
    public string? MacAddress { get; set; }
    public string? IpAddress { get; set; }
    
    public Guid? OwnerId { get; set; }
    public Guid? LocationId { get; set; }
    
    // Navigation
    public User? Owner { get; set; }
    public Location? Location { get; set; }
}
