using System;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using Xunit;

namespace OmniFlow.Application.UnitTests.Tenants
{
    public class Phase4StudioAndSlaTests
    {
        [Fact]
        public void CustomDepartment_Initialization_IsCorrect()
        {
            var dept = new CustomDepartment
            {
                Code = "LEGAL",
                Name = "Legal & Contract Management",
                DepartmentType = "Legal",
                ColorHex = "#f59e0b",
                IsActive = true
            };

            Assert.Equal("LEGAL", dept.Code);
            Assert.Equal("Legal & Contract Management", dept.Name);
            Assert.Equal("Legal", dept.DepartmentType);
            Assert.True(dept.IsActive);
        }

        [Fact]
        public void SlaPolicyRule_EscalationThreshold_IsConfigurable()
        {
            var rule = new SlaPolicyRule
            {
                Name = "P1 Outage Policy",
                Priority = TicketPriority.Critical,
                ResponseTimeMinutes = 15,
                ResolutionTimeHours = 4,
                EscalateAfterPercent = 75,
                NotifyManagerOnBreach = true
            };

            Assert.Equal(TicketPriority.Critical, rule.Priority);
            Assert.Equal(75, rule.EscalateAfterPercent);
            Assert.True(rule.NotifyManagerOnBreach);
        }

        [Fact]
        public void TicketFormSchema_FieldsJson_Validates()
        {
            var schema = new TicketFormSchema
            {
                FormTitle = "Hardware Acquisition",
                FieldsJson = """[{"key":"device","fieldType":"Select"}]"""
            };

            Assert.Equal("Hardware Acquisition", schema.FormTitle);
            Assert.Contains("device", schema.FieldsJson);
        }
    }
}
