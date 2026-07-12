using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class Benefit : AuditableEntity
    {
        public Guid EmployeeId { get; set; }
        public string BenefitType { get; set; } = string.Empty; // e.g., Health, Retirement
        public DateTime EligibilityDate { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Employee? Employee { get; set; }
    }
}
