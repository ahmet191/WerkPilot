using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class EmployeeSkill : AuditableEntity
    {
        public Guid EmployeeUserId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public string Category { get; set; } = "Technical"; // "Technical", "Leadership", "Domain", "Compliance"
        public int ProficiencyLevel { get; set; } = 3; // 1 (Beginner) to 5 (Expert)
        public DateTime LastEvaluatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User? EmployeeUser { get; set; }
    }
}
