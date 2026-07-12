using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities.HR;

namespace OmniFlow.Domain.Entities;

public class Department : AuditableEntity
{
    
    // Multi-lingual name (e.g., {"en": "Human Resources", "tr": "İnsan Kaynakları"})
    public MultiLingualString LocalizedName { get; set; } = new();
    
    public Guid? ManagerId { get; set; }
    public Guid? HeadId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public string? CostCenterCode { get; set; }
    
    // Navigation
    public User? Manager { get; set; }
    public Employee? Head { get; set; }
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> ChildDepartments { get; set; } = new List<Department>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}
