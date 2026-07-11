using MediatR;
using Microsoft.EntityFrameworkCore;
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
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null) throw new Exception("Ticket not found.");

        if (request.AssigneeId.HasValue)
        {
            var assignee = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == request.AssigneeId, cancellationToken);
            if (assignee == null) throw new Exception("Assignee user not found.");
        }

        var oldAssignee = ticket.AssigneeId;
        ticket.AssigneeId = request.AssigneeId;

        // Write audit log
        var audit = new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            ChangedById = request.ChangedById,
            ChangedAt = DateTimeOffset.UtcNow,
            Action = "Assign",
            FieldName = "AssigneeId",
            OldValue = oldAssignee?.ToString(),
            NewValue = request.AssigneeId?.ToString()
        };

        _context.Set<TicketAuditLog>().Add(audit);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
