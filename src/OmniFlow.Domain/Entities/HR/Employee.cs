using System;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Domain.Entities.HR
{
    public class Employee : AuditableEntity
    {
        public Guid UserId { get; set; }
        public Guid? DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime HireDate { get; set; }
        public DateTime? ContractStart { get; set; }
        public DateTime? ContractEnd { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Department? Department { get; set; }
        public Position? Position { get; set; }
    }
}
