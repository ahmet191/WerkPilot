using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Commands;

public class AddTicketCommentCommandHandler : IRequestHandler<AddTicketCommentCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;

    public AddTicketCommentCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(AddTicketCommentCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);
        if (ticket == null) throw new Exception("Ticket not found.");

        var user = await _context.Set<User>().FirstOrDefaultAsync(u => u.Id == request.AuthorId, cancellationToken);
        if (user == null) throw new Exception("Author not found.");

        var comment = new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            AuthorId = request.AuthorId,
            Content = request.Content
        };

        // Create an audit log record for this comment action
        var audit = new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = request.TicketId,
            ChangedById = request.AuthorId,
            ChangedAt = DateTimeOffset.UtcNow,
            Action = "AddComment",
            FieldName = "Comment",
            NewValue = request.Content
        };

        _context.Set<TicketComment>().Add(comment);
        _context.Set<TicketAuditLog>().Add(audit);
        
        await _context.SaveChangesAsync(cancellationToken);

        return comment.Id;
    }
}
