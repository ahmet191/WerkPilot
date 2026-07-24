using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common.Exceptions;
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
        var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ticket), request.TicketId);

        var authorExists = await _context.Set<User>().AnyAsync(u => u.Id == request.AuthorId, cancellationToken);
        if (!authorExists) throw new NotFoundException(nameof(User), request.AuthorId);

        // The aggregate creates the comment and its audit entry.
        var comment = ticket.AddComment(request.AuthorId, request.Content);

        await _context.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }
}
