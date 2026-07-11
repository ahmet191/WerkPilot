using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class DirectoryProfile : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Bio { get; set; }
    public List<string> Skills { get; set; } = new();
    
    public bool ShowPhoneNumber { get; set; } = true;
    public bool ShowPersonalEmail { get; set; } = false;
    public bool ShowDateOfBirth { get; set; } = false;
    
    // Navigation
    public User User { get; set; } = null!;
}
