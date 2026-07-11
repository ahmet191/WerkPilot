namespace OmniFlow.Domain.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty; // e.g., acmecorp.omniflow.com
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
