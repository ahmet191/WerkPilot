using MediatR;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Notifications;

public class TicketCreatedNotificationHandler : INotificationHandler<TicketCreatedDomainEvent>
{
    private readonly IMessageGateway _messageGateway;
    private readonly ITelegramIdentityService _identityService;
    private readonly IDashboardMetricsService _dashboardMetricsService;
    private readonly ILogger<TicketCreatedNotificationHandler> _logger;

    public TicketCreatedNotificationHandler(
        IMessageGateway messageGateway,
        ITelegramIdentityService identityService,
        IDashboardMetricsService dashboardMetricsService,
        ILogger<TicketCreatedNotificationHandler> logger)
    {
        _messageGateway = messageGateway;
        _identityService = identityService;
        _dashboardMetricsService = dashboardMetricsService;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;
        _logger.LogInformation("Domain Event Handler triggered for Ticket: {TicketId}", ticket.Id);

        // Resolve Telegram Chat ID for this user
        var chatId = await _identityService.GetTelegramChatIdByUserIdAsync(ticket.RequesterId, cancellationToken);
        if (!string.IsNullOrEmpty(chatId))
        {
            var msg = $"🔔 **New Ticket Created!**\n" +
                      $"ID: `{ticket.Id}`\n" +
                      $"State: `{ticket.CurrentState}`\n" +
                      $"We are processing your request.";

            await _messageGateway.SendMessageAsync(chatId, msg, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No Telegram Chat ID mapped for user {UserId}. Cannot send notification.", ticket.RequesterId);
        }

        // 3. Update Dashboard Live Metrics
        await _dashboardMetricsService.BroadcastMetricUpdateAsync("open_tickets", 1, cancellationToken);
    }
}
