using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OmniFlow.Domain.Entities;

public class AppMenu : OmniFlow.Domain.Common.AuditableEntity
{
    public Guid? ParentId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TitleKey { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string? Path { get; set; }
    
    [MaxLength(100)]
    public string? Icon { get; set; }
    
    public int OrderIndex { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public string? RequiredRoles { get; set; } // Comma separated roles, e.g., "Admin,Manager"

    // Navigation properties
    public AppMenu? Parent { get; set; }
    public ICollection<AppMenu> Children { get; set; } = new List<AppMenu>();
}
