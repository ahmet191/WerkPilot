using MediatR;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Notifications;

public class TicketStateTransitionedNotificationHandler : INotificationHandler<TicketStateTransitionedDomainEvent>
{
    private readonly IMessageGateway _messageGateway;
    private readonly ITelegramIdentityService _identityService;
    private readonly ILogger<TicketStateTransitionedNotificationHandler> _logger;

    public TicketStateTransitionedNotificationHandler(
        IMessageGateway messageGateway,
        ITelegramIdentityService identityService,
        ILogger<TicketStateTransitionedNotificationHandler> logger)
    {
        _messageGateway = messageGateway;
        _identityService = identityService;
        _logger = logger;
    }

    public async Task Handle(TicketStateTransitionedDomainEvent notification, CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;
        _logger.LogInformation("Domain Event: State Transitioned triggered for Ticket {TicketId}", ticket.Id);

        var chatId = await _identityService.GetTelegramChatIdByUserIdAsync(ticket.RequesterId, cancellationToken);
        if (!string.IsNullOrEmpty(chatId))
        {
            var msg = $"🔄 **Ticket Status Update!**\n" +
                      $"ID: `{ticket.Id}`\n" +
                      $"Transition: `{notification.PreviousState}` ➡️ `{notification.NewState}` (via `{notification.Trigger}`)";

            await _messageGateway.SendMessageAsync(chatId, msg, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No Telegram Chat ID mapped for user {UserId}. Cannot send transition notification.", ticket.RequesterId);
        }
    }
}
