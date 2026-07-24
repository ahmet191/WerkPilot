using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Analytics.Queries
{
    public record GetTicketsByStatusQuery(Guid TenantId) : IRequest<List<object>>;
    public record GetTicketsByPriorityQuery(Guid TenantId) : IRequest<List<object>>;
    public record GetVolumeTrendQuery(Guid TenantId, int Days) : IRequest<List<object>>;
    public record GetSlaComplianceQuery(Guid TenantId) : IRequest<object>;
    public record GetTeamPerformanceQuery(Guid TenantId) : IRequest<List<object>>;

    public class GetBasicReportsQueryHandler : 
        IRequestHandler<GetTicketsByStatusQuery, List<object>>,
        IRequestHandler<GetTicketsByPriorityQuery, List<object>>,
        IRequestHandler<GetVolumeTrendQuery, List<object>>,
        IRequestHandler<GetSlaComplianceQuery, object>,
        IRequestHandler<GetTeamPerformanceQuery, List<object>>
    {
        private readonly IOmniFlowDbContext _context;

        public GetBasicReportsQueryHandler(IOmniFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<object>> Handle(GetTicketsByStatusQuery request, CancellationToken cancellationToken)
        {
            var data = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId)
                .GroupBy(t => t.CurrentState)
                .Select(g => new { status = g.Key.ToString(), count = g.Count() })
                .ToListAsync(cancellationToken);

            return data.Cast<object>().ToList();
        }

        public async Task<List<object>> Handle(GetTicketsByPriorityQuery request, CancellationToken cancellationToken)
        {
            var data = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId)
                .GroupBy(t => t.Priority)
                .Select(g => new { priority = g.Key.ToString(), count = g.Count() })
                .ToListAsync(cancellationToken);

            return data.Cast<object>().ToList();
        }

        public async Task<List<object>> Handle(GetVolumeTrendQuery request, CancellationToken cancellationToken)
        {
            var cutoff = DateTimeOffset.UtcNow.AddDays(-request.Days);
            var tickets = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId && t.CreatedAt >= cutoff)
                .ToListAsync(cancellationToken);

            var ticketsByDate = tickets
                .GroupBy(t => t.CreatedAt.ToString("yyyy-MM-dd"))
                .ToDictionary(g => g.Key, g => g.Count());

            // Fill every day in the range so the chart always shows the full period
            var result = new List<object>();
            for (var i = request.Days - 1; i >= 0; i--)
            {
                var day = DateTimeOffset.UtcNow.AddDays(-i).ToString("yyyy-MM-dd");
                result.Add(new { date = day, count = ticketsByDate.GetValueOrDefault(day, 0) });
            }

            return result;
        }

        public async Task<object> Handle(GetSlaComplianceQuery request, CancellationToken cancellationToken)
        {
            var tickets = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId)
                .ToListAsync(cancellationToken);

            var total = tickets.Count;
            if (total == 0) return new { total = 0, compliant = 0, breached = 0, compliancePct = 0.0 };

            var breached = tickets.Count(t => t.SlaBreachNotifiedAt.HasValue);
            var compliant = total - breached;
            var pct = Math.Round((double)compliant / total * 100, 2);

            return new { total, compliant, breached, compliancePct = pct };
        }

        public async Task<List<object>> Handle(GetTeamPerformanceQuery request, CancellationToken cancellationToken)
        {
            var tickets = await _context.Tickets
                .Where(t => t.TenantId == request.TenantId && t.AssigneeId != null)
                .ToListAsync(cancellationToken);

            var users = await _context.Users.ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

            var grouped = tickets
                .GroupBy(t => t.AssigneeId)
                .Select(g => new {
                    agent = users.ContainsKey(g.Key!.Value) ? users[g.Key!.Value] : "Unknown",
                    assigned = g.Count(),
                    open = g.Count(t => t.CurrentState != TicketState.Resolved && t.CurrentState != TicketState.Closed),
                    resolved = g.Count(t => t.CurrentState == TicketState.Resolved || t.CurrentState == TicketState.Closed)
                })
                .Cast<object>()
                .ToList();

            return grouped;
        }
    }
}
