using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

public class TicketSlaBreachConsumer : IConsumer<CheckTicketSlaBreachEvent>
{
    private readonly IOmniFlowDbContext _dbContext;
    private readonly IMessageGateway _messageGateway;
    private readonly ILogger<TicketSlaBreachConsumer> _logger;

    public TicketSlaBreachConsumer(
        IOmniFlowDbContext dbContext,
        IMessageGateway messageGateway,
        ILogger<TicketSlaBreachConsumer> logger)
    {
        _dbContext = dbContext;
        _messageGateway = messageGateway;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CheckTicketSlaBreachEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing CheckTicketSlaBreachEvent for TicketId: {TicketId}", evt.TicketId);

        var ticket = await _dbContext.Tickets
            .Include(t => t.Assignee)
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.Id == evt.TicketId, context.CancellationToken);

        if (ticket == null) return;

        // Verify if it's actually breached
        // If it's already Resolved or Closed, we ignore it.
        if (ticket.CurrentState == TicketState.Resolved || ticket.CurrentState == TicketState.Closed)
        {
            _logger.LogInformation("Ticket {TicketId} is already resolved. Ignoring SLA Breach alert.", evt.TicketId);
            return;
        }

        var alertChatId = ticket.Assignee?.TelegramChatId;
        if (string.IsNullOrEmpty(alertChatId))
        {
            _logger.LogWarning("SLA breach on ticket {TicketId} but no assignee with a linked Telegram chat; skipping notification.", ticket.Id);
            return;
        }

        var msg = $"🚨 SLA BREACH ALERT 🚨\nTicket '{ticket.Title}' (ID: {ticket.Id}) has breached its resolution SLA!\nCurrent State: {ticket.CurrentState}\nAssignee: {ticket.Assignee?.Name ?? "Unassigned"}";

        await _messageGateway.SendMessageAsync(alertChatId, msg, context.CancellationToken);
    }
}
