using MediatR;

namespace OmniFlow.Application.Tickets.Commands;

public record AssignTicketCommand(
    Guid TicketId,
    Guid? AssigneeId,
    Guid ChangedById) : IRequest<bool>;
