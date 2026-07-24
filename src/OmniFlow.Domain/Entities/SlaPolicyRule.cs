using System;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities
{
    public class SlaPolicyRule : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
        public int ResponseTimeMinutes { get; set; } = 60;
        public int ResolutionTimeHours { get; set; } = 24;
        public int EscalateAfterPercent { get; set; } = 80; // Triggers escalation alert at 80% SLA time elapsed
        public Guid? EscalateToUserId { get; set; }
        public bool NotifyManagerOnBreach { get; set; } = true;
        public bool IsActive { get; set; } = true;

        // Navigation
        public User? EscalateToUser { get; set; }
    }
}
