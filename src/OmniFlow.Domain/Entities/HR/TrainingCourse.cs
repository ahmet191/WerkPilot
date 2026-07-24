using System;
using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities.HR
{
    public enum TrainingStatus
    {
        NotStarted = 1,
        InProgress = 2,
        Completed = 3
    }

    public class TrainingCourse : AuditableEntity
    {
        public Guid EmployeeUserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = "Mandatory Compliance"; // "Mandatory Compliance", "Technical", "Soft Skills"
        public int DurationHours { get; set; } = 4;
        public bool IsMandatory { get; set; } = true;
        public TrainingStatus Status { get; set; } = TrainingStatus.NotStarted;
        public DateTime? CompletedAt { get; set; }
        public int Score { get; set; } = 0; // Out of 100

        // Navigation
        public User? EmployeeUser { get; set; }
    }
}
