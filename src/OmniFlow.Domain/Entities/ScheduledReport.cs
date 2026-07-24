using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities
{
    public class ScheduledReport : AuditableEntity
    {
        public string ReportName { get; set; } = string.Empty;
        
        /// <summary>
        /// Cron expression for scheduling (e.g. "0 8 * * 1" for every Monday at 8 AM)
        /// </summary>
        public string CronExpression { get; set; } = string.Empty;
        
        /// <summary>
        /// Comma-separated list of recipient email addresses.
        /// </summary>
        public string Recipients { get; set; } = string.Empty;
        
        /// <summary>
        /// The format of the report export (e.g. "PDF", "Excel")
        /// </summary>
        public string Format { get; set; } = "PDF";
        
        public DateTime? NextRunTime { get; set; }
        public DateTime? LastRunTime { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
