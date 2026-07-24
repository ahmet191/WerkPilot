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

        // In a real scenario, this could query the Department Manager's ChatId or an IT Support Channel ID.
        // We'll use our mock default Chat ID for demo purposes.
        var itSupportChatId = "8920226584";

        var msg = $"🆕 *New Ticket Created*\n" +
                  $"Title: {ticket.Title}\n" +
                  $"Ticket ID: {ticket.Id}\n" +
                  $"Priority: High\n" +
                  $"Link: https://omniflow.local/tickets/{ticket.Id}";

        await _messageGateway.SendMessageAsync(itSupportChatId, msg, cancellationToken);
    }
}
