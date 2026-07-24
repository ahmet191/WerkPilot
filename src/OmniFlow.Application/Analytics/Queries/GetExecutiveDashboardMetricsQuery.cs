using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Analytics.Queries
{
    public class ExecutiveDashboardMetricsDto
    {
        public int TotalActiveTickets { get; set; }
        public double AverageResolutionTimeHours { get; set; }
        public double FirstContactResolutionPercentage { get; set; }
        public double SlaBreachRatePercentage { get; set; }
        public int AiPredictionsCount { get; set; }
    }

    public record GetExecutiveDashboardMetricsQuery(Guid TenantId) : IRequest<ExecutiveDashboardMetricsDto>;

    public class GetExecutiveDashboardMetricsQueryHandler : IRequestHandler<GetExecutiveDashboardMetricsQuery, ExecutiveDashboardMetricsDto>
    {
        private readonly IOmniFlowDbContext _context;

        public GetExecutiveDashboardMetricsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<ExecutiveDashboardMetricsDto> Handle(GetExecutiveDashboardMetricsQuery request, CancellationToken cancellationToken)
        {
            var tickets = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var activeTicketsCount = tickets.Count(t => t.CurrentState != TicketState.Closed && t.CurrentState != TicketState.Resolved);
            var resolvedTickets = tickets.Where(t => t.CurrentState == TicketState.Resolved || t.CurrentState == TicketState.Closed).ToList();

            double avgResolution = 0;
            var resolvedWithDate = resolvedTickets.Where(t => t.ResolutionDate.HasValue).ToList();
            if (resolvedWithDate.Any())
            {
                avgResolution = resolvedWithDate.Average(t => (t.ResolutionDate!.Value - t.CreatedAt).TotalHours);
            }

            // First-time resolution: resolved tickets that were closed without ever
            // breaching their SLA target. Uses the same breach signal the SLA
            // compliance report does, so the two figures always agree.
            double fcrRate = 0;
            if (resolvedTickets.Any())
            {
                var firstTimeResolved = resolvedTickets.Count(t => !t.SlaBreachNotifiedAt.HasValue);
                fcrRate = (double)firstTimeResolved / resolvedTickets.Count * 100;
            }

            // Real SLA breach rate: the share of all tickets whose breach job fired.
            var breachedCount = tickets.Count(t => t.SlaBreachNotifiedAt.HasValue);
            double slaBreachRate = tickets.Any() ? (double)breachedCount / tickets.Count * 100 : 0;

            var aiPredictionsCount = await _context.TicketAiPredictions
                .Where(p => p.TenantId == request.TenantId)
                .CountAsync(cancellationToken);

            return new ExecutiveDashboardMetricsDto
            {
                TotalActiveTickets = activeTicketsCount,
                AverageResolutionTimeHours = Math.Round(avgResolution, 1),
                FirstContactResolutionPercentage = Math.Round(fcrRate, 1),
                SlaBreachRatePercentage = Math.Round(slaBreachRate, 1),
                AiPredictionsCount = aiPredictionsCount
            };
        }
    }
}
