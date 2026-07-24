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
    private readonly INotificationService _notifications;
    private readonly ITenantService _tenantService;
    private readonly ILogger<TicketSlaBreachConsumer> _logger;

    public TicketSlaBreachConsumer(
        IOmniFlowDbContext dbContext,
        INotificationService notifications,
        ITenantService tenantService,
        ILogger<TicketSlaBreachConsumer> logger)
    {
        _dbContext = dbContext;
        _notifications = notifications;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CheckTicketSlaBreachEvent> context)
    {
        var evt = context.Message;
        // No HttpContext here: scope the DbContext to the event's tenant.
        _tenantService.SetTenantId(evt.TenantId);
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

        // Idempotency guard: the sweep service and the scheduler can both fire
        // for the same ticket — only ever alert once.
        if (ticket.SlaBreachNotifiedAt.HasValue)
        {
            return;
        }

        ticket.SlaBreachNotifiedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        if (ticket.AssigneeId is null)
        {
            _logger.LogWarning("SLA breach on ticket {TicketId} but it is unassigned; nobody to notify.", ticket.Id);
            return;
        }

        await _notifications.NotifyUserAsync(
            ticket.AssigneeId.Value,
            NotificationKind.SlaBreached,
            "SLA breached",
            $"Ticket '{ticket.Title}' has passed its resolution SLA (state: {ticket.CurrentState}).",
            "/tickets",
            context.CancellationToken);
    }
}
