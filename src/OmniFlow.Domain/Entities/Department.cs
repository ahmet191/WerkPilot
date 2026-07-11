using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class Department : AuditableEntity
{
    public Guid Id { get; set; }
    
    // Multi-lingual name (e.g., {"en": "Human Resources", "tr": "İnsan Kaynakları"})
    public MultiLingualString LocalizedName { get; set; } = new();
    
    public Guid? ManagerId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    
    // Navigation
    public User? Manager { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
