using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class PayrollRun : AuditableEntity
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
