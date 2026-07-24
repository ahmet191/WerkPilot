using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Application.Tickets.EventHandlers;

public class TicketCreatedEventHandler : INotificationHandler<TicketCreatedDomainEvent>
{
    private readonly IMessageGateway _messageGateway;

    public TicketCreatedEventHandler(IMessageGateway messageGateway)
    {
        _messageGateway = messageGateway;
    }

    public async Task Handle(TicketCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;

        var msg = $"🆕 *New Ticket Created*\n" +
                  $"Title: {ticket.Title}\n" +
                  $"Ticket ID: {ticket.Id}\n" +
                  $"Priority: {ticket.Priority}\n" +
                  $"Link: https://omniflow.local/tickets/{ticket.Id}";

        // Routes to the tenant's configured support channel; no-ops if none is set.
        await _messageGateway.SendToSupportChannelAsync(msg, cancellationToken);
    }
}
