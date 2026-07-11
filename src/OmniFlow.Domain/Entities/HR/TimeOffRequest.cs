using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class TimeOffRequest : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., Vacation, Sick
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public Guid? ApproverId { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public Employee? Employee { get; set; }
        public Employee? Approver { get; set; }
    }
}
