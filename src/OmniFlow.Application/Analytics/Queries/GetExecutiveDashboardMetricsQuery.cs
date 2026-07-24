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
            if (resolvedTickets.Any())
            {
                avgResolution = resolvedTickets
                    .Where(t => t.ResolutionDate.HasValue)
                    .Average(t => (t.ResolutionDate!.Value - t.CreatedAt).TotalHours);
            }

            double fcrRate = 0; // Simulated FCR logic for now
            if (resolvedTickets.Any())
            {
                // Fake condition: resolved within 4 hours
                var fcrCount = resolvedTickets.Count(t => t.ResolutionDate.HasValue && (t.ResolutionDate!.Value - t.CreatedAt).TotalHours < 4);
                fcrRate = (double)fcrCount / resolvedTickets.Count * 100;
            }

            // Fake SLA logic for demonstration
            double slaBreachRate = resolvedTickets.Any() ? 5.2 : 0; 
            
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
