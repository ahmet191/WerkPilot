using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class Location : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "HQ - London", "Istanbul Office"
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string TimeZone { get; set; } = "UTC";
}
