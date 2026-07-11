using MediatR;

namespace OmniFlow.Application.Tickets.Commands;

public record TransitionTicketCommand(
    Guid TicketId,
    string Trigger,
    Guid ChangedById) : IRequest<bool>;
