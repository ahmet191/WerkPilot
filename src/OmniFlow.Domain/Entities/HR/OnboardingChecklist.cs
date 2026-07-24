using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public enum ChecklistType
    {
        Onboarding = 1,
        Offboarding = 2
    }

    public enum ChecklistDepartment
    {
        IT = 1,
        HR = 2,
        Facilities = 3,
        Finance = 4
    }

    public class OnboardingChecklist : AuditableEntity
    {
        public Guid EmployeeUserId { get; set; }
        public ChecklistType Type { get; set; } = ChecklistType.Onboarding;
        public string TaskName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ChecklistDepartment AssignedDepartment { get; set; } = ChecklistDepartment.IT;
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        public Guid? CompletedByUserId { get; set; }
        public DateTime? DueDate { get; set; }

        // Navigation
        public User? EmployeeUser { get; set; }
        public User? CompletedByUser { get; set; }
    }
}
