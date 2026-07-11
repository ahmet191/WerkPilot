using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Dashboard.Queries;

public record GetMetricsQuery() : IRequest<Dictionary<string, int>>;

public class GetMetricsQueryHandler : IRequestHandler<GetMetricsQuery, Dictionary<string, int>>
{
    private readonly IOmniFlowDbContext _context;

    public GetMetricsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, int>> Handle(GetMetricsQuery request, CancellationToken cancellationToken)
    {
        var openTicketsCount = await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.CurrentState != TicketState.Resolved && t.CurrentState != TicketState.Closed, cancellationToken);

        var pendingLeavesCount = await _context.LeaveRequests
            .AsNoTracking()
            .CountAsync(l => l.Status == LeaveStatus.Pending, cancellationToken);

        var slaBreachesCount = await _context.Tickets
            .AsNoTracking()
            .CountAsync(t => t.SlaDueDate.HasValue && t.SlaDueDate < DateTimeOffset.UtcNow && t.CurrentState != TicketState.Resolved && t.CurrentState != TicketState.Closed, cancellationToken);

        var myAssetsCount = 0; // We can get this from context user id, but for now we'll just return 0 to keep it simple.

        return new Dictionary<string, int>
        {
            { "open_tickets", openTicketsCount },
            { "pending_leaves", pendingLeavesCount },
            { "sla_breaches", slaBreachesCount },
            { "my_assets", myAssetsCount }
        };
    }
}
