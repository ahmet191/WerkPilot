using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class TicketCategory : AuditableEntity
{
    
    // Multi-lingual name (e.g., {"en": "Hardware Issue", "tr": "Donanım Sorunu"})
    public MultiLingualString LocalizedName { get; set; } = new();
    
    public Guid DepartmentId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    
    // Navigation
    public Department Department { get; set; } = null!;
    public TicketCategory? ParentCategory { get; set; }
    public ICollection<TicketCategory> SubCategories { get; set; } = new List<TicketCategory>();
}
