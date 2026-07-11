using MediatR;

namespace OmniFlow.Application.Tickets.Commands;

public record CreateTicketCommand(
    Guid TemplateId,
    Guid RequesterId,
    Dictionary<string, object> DynamicData) : IRequest<Guid>;
