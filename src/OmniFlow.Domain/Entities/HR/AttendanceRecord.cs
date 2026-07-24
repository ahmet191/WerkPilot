using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class AttendanceRecord : AuditableEntity
    {
        public Guid EmployeeId { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }

        public Employee? Employee { get; set; }
    }
}
