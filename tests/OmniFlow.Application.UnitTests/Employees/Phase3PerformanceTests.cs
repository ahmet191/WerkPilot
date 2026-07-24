using System;
using OmniFlow.Application.Employees.Queries;
using OmniFlow.Domain.Entities.HR;
using Xunit;

namespace OmniFlow.Application.UnitTests.Employees
{
    public class Phase3PerformanceTests
    {
        [Fact]
        public void EmployeeGoal_ProgressCalculation_IsCorrect()
        {
            var goal = new EmployeeGoal
            {
                Title = "Achieve >98% SLA",
                TargetValue = 100m,
                CurrentValue = 85m,
                Quarter = GoalQuarter.Q1,
                Year = 2026
            };

            Assert.Equal(85m, goal.ProgressPercent);
            Assert.Equal(GoalQuarter.Q1, goal.Quarter);
        }

        [Fact]
        public void EmployeeCertificate_ExpiryCheck_Works()
        {
            var certExpired = new EmployeeCertificate
            {
                CertificateName = "ISO 27001 Auditor",
                ExpirationDate = DateTime.UtcNow.AddDays(-5)
            };

            var certExpiringSoon = new EmployeeCertificate
            {
                CertificateName = "AWS Solutions Architect",
                ExpirationDate = DateTime.UtcNow.AddDays(15)
            };

            Assert.True(certExpired.IsExpired);
            Assert.True(certExpiringSoon.IsExpiringSoon);
            Assert.False(certExpiringSoon.IsExpired);
        }

        [Fact]
        public void TrainingCourse_DefaultStatus_IsNotStarted()
        {
            var course = new TrainingCourse
            {
                Title = "2026 GDPR & KVKK Security Training",
                IsMandatory = true
            };

            Assert.Equal(TrainingStatus.NotStarted, course.Status);
            Assert.True(course.IsMandatory);
        }
    }
}
