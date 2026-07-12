using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Queries;

public class GetTicketsQueryHandler :
    IRequestHandler<GetTicketsQuery, List<TicketDto>>,
    IRequestHandler<GetTicketsByServiceDeskQuery, List<TicketDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetTicketsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<TicketDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        return await ProjectAsync(
            _context.Tickets.AsNoTracking().Where(t => t.RequesterId == request.RequesterId),
            cancellationToken);
    }

    public async Task<List<TicketDto>> Handle(GetTicketsByServiceDeskQuery request, CancellationToken cancellationToken)
    {
        return await ProjectAsync(
            _context.Tickets.AsNoTracking().Where(t => t.ServiceDeskId == request.ServiceDeskId),
            cancellationToken);
    }

    private static Task<List<TicketDto>> ProjectAsync(IQueryable<Ticket> query, CancellationToken cancellationToken) =>
        query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto
            {
                Id = t.Id,
                Title = t.Title,
                Type = t.Type.ToString(),
                Priority = t.Priority.ToString(),
                Impact = t.Impact.ToString(),
                Urgency = t.Urgency.ToString(),
                CurrentState = t.CurrentState.ToString(),
                RequesterId = t.RequesterId,
                RequesterName = t.Requester != null ? t.Requester.Name : string.Empty,
                AssigneeName = t.Assignee != null ? t.Assignee.Name : string.Empty,
                SlaDueDate = t.SlaDueDate,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);
}
