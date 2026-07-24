using MediatR;

namespace OmniFlow.Application.Tickets.Commands;

public record AddTicketCommentCommand(
    Guid TicketId,
    Guid AuthorId,
    string Content) : IRequest<Guid>;
