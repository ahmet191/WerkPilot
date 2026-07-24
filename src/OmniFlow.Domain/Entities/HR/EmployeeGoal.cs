using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public enum GoalQuarter
    {
        Q1 = 1,
        Q2 = 2,
        Q3 = 3,
        Q4 = 4,
        Annual = 5
    }

    public enum GoalStatus
    {
        InProgress = 1,
        Achieved = 2,
        Behind = 3,
        Cancelled = 4
    }

    public class EmployeeGoal : AuditableEntity
    {
        public Guid EmployeeUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public GoalQuarter Quarter { get; set; } = GoalQuarter.Q1;
        public int Year { get; set; } = 2026;
        public decimal TargetValue { get; set; } = 100m;
        public decimal CurrentValue { get; set; } = 0m;
        public string Unit { get; set; } = "Percentage"; // "Percentage", "Currency", "Count"
        public GoalStatus Status { get; set; } = GoalStatus.InProgress;

        public decimal ProgressPercent => TargetValue > 0 ? Math.Min(100m, Math.Round((CurrentValue / TargetValue) * 100m, 1)) : 0m;

        // Navigation
        public User? EmployeeUser { get; set; }
    }
}
