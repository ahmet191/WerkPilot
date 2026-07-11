using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Queries;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, List<Ticket>>
{
    private readonly IOmniFlowDbContext _context;

    public GetTicketsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<Ticket>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.RequesterId == request.RequesterId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
