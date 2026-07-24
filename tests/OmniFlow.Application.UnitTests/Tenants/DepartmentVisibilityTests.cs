using System;
using OmniFlow.Application.Tenants.Queries;
using OmniFlow.Domain.Entities;
using Xunit;

namespace OmniFlow.Application.UnitTests.Tenants
{
    public class DepartmentVisibilityTests
    {
        [Fact]
        public void DepartmentVisibility_Initialization_SetsDefaults()
        {
            var vis = new DepartmentVisibility
            {
                RoleName = "Requester",
                ServiceDeskKey = "HR",
                IsVisible = true,
                CanCreateTickets = true,
                IsConfidential = true
            };

            Assert.Equal("Requester", vis.RoleName);
            Assert.Equal("HR", vis.ServiceDeskKey);
            Assert.True(vis.IsVisible);
            Assert.True(vis.CanCreateTickets);
            Assert.True(vis.IsConfidential);
        }

        [Fact]
        public void ServiceDeskCardDto_Mapping_Works()
        {
            var card = new ServiceDeskCardDto
            {
                Id = Guid.NewGuid(),
                Name = "Legal & Compliance Desk",
                DepartmentType = "Legal",
                IsConfidential = true,
                IconName = "FileTextOutlined",
                ColorHex = "#fa8c16",
                CanCreateTickets = true
            };

            Assert.Equal("Legal & Compliance Desk", card.Name);
            Assert.Equal("Legal", card.DepartmentType);
            Assert.True(card.IsConfidential);
            Assert.Equal("#fa8c16", card.ColorHex);
        }
    }
}
