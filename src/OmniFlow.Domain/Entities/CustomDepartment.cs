using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public class CustomDepartment : AuditableEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DepartmentType { get; set; } = "Custom"; // "IT", "HR", "Finance", "Legal", "Procurement", "Facilities", "Custom"
        public string IconName { get; set; } = "BankOutlined";
        public string ColorHex { get; set; } = "#8b5cf6";
        public Guid? ParentDepartmentId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public CustomDepartment? ParentDepartment { get; set; }
    }
}
