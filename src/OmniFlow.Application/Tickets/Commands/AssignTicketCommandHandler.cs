using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Commands;

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand, bool>
{
    private readonly IOmniFlowDbContext _context;

    public AssignTicketCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ticket), request.TicketId);

        if (request.AssigneeId.HasValue)
        {
            var assigneeExists = await _context.Set<User>().AnyAsync(u => u.Id == request.AssigneeId, cancellationToken);
            if (!assigneeExists) throw new NotFoundException(nameof(User), request.AssigneeId);
        }

        // The aggregate updates the assignee and records the audit entry.
        ticket.Assign(request.AssigneeId, request.ChangedById);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
