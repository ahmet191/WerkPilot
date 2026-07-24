using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public class DepartmentVisibility : AuditableEntity
    {
        public string RoleName { get; set; } = string.Empty;
        public string ServiceDeskKey { get; set; } = string.Empty; // "IT", "HR", "FINANCE", "LEGAL", "FACILITIES"
        public bool IsVisible { get; set; } = true;
        public bool CanCreateTickets { get; set; } = true;
        public bool IsConfidential { get; set; } = false;
    }
}
