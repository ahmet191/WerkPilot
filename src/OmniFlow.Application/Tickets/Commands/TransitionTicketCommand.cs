using MediatR;

namespace OmniFlow.Application.Tickets.Commands;

/// <param name="IsSystem">
/// True only for transitions fired by the platform itself (e.g. the approval
/// engine applying an outcome). Never bindable from the API surface — the
/// controller builds this command from a DTO without the flag.
/// </param>
public record TransitionTicketCommand(
    Guid TicketId,
    string Trigger,
    Guid ChangedById,
    bool IsSystem = false) : IRequest<bool>;
