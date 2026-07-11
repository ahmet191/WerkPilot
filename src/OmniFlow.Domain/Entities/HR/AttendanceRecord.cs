using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class AttendanceRecord : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public bool IsDeleted { get; set; } = false;

        public Employee? Employee { get; set; }
    }
}
