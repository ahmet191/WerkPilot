using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public class Position : AuditableEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal SalaryBand { get; set; }
    }
}
