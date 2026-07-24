using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Analytics.Commands;
using OmniFlow.Application.Analytics.Queries;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AnalyticsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentTenantId =>
            Guid.TryParse(User.FindFirstValue("tenant_id"), out var id)
                ? id
                : Guid.Parse("00000000-0000-0000-0000-000000000001");

        [HttpGet("dashboard")]
        [Authorize(Policy = PermissionPolicyNames.ReportsManage)]
        public async Task<ActionResult<ExecutiveDashboardMetricsDto>> GetDashboardMetrics(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetExecutiveDashboardMetricsQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports")]
        [Authorize(Policy = PermissionPolicyNames.ReportsManage)]
        public async Task<ActionResult<List<ScheduledReportDto>>> GetScheduledReports(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetScheduledReportsQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpPost("reports")]
        [Authorize(Policy = PermissionPolicyNames.ReportsManage)]
        public async Task<ActionResult<Guid>> CreateScheduledReport([FromBody] CreateScheduledReportCommand command, CancellationToken cancellationToken)
        {
            command.TenantId = CurrentTenantId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        // ── Basic report endpoints consumed by ReportsPage.tsx ──

        [HttpGet("reports/tickets-by-status")]
        public async Task<IActionResult> GetTicketsByStatus(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketsByStatusQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/tickets-by-priority")]
        public async Task<IActionResult> GetTicketsByPriority(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketsByPriorityQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/volume-trend")]
        public async Task<IActionResult> GetVolumeTrend([FromQuery] int days = 30, CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(new GetVolumeTrendQuery(CurrentTenantId, days), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/sla-compliance")]
        public async Task<IActionResult> GetSlaCompliance(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSlaComplianceQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/team-performance")]
        public async Task<IActionResult> GetTeamPerformance(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTeamPerformanceQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        [HttpGet("reports/export/tickets.csv")]
        public async Task<IActionResult> ExportTicketsCsv(CancellationToken cancellationToken)
        {
            var tickets = await _mediator.Send(new GetTicketsByStatusQuery(CurrentTenantId), cancellationToken);
            var csv = "Status,Count\n" + string.Join("\n", tickets.Select(t => t.ToString()));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "tickets.csv");
        }
    }
}
