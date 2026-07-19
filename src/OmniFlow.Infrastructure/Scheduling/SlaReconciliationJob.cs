using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;
using OmniFlow.Infrastructure.Persistence;
using Quartz;

namespace OmniFlow.Infrastructure.Scheduling;

/// <summary>
/// Safety-net reconciliation for SLA breach detection. The primary mechanism
/// is the exact-time <c>CheckTicketSlaBreachEvent</c> scheduled through the
/// Quartz-backed message scheduler at ticket creation; this job sweeps for
/// anything that slipped through (schedule write failures, tickets whose SLA
/// changed after creation). The consumer's <c>SlaBreachNotifiedAt</c> guard
/// keeps alerts idempotent when both mechanisms fire.
/// </summary>
[DisallowConcurrentExecution]
public class SlaReconciliationJob : IJob
{
    public static readonly JobKey Key = new("sla-reconciliation", "omniflow");

    private readonly OmniFlowDbContext _dbContext;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<SlaReconciliationJob> _logger;

    public SlaReconciliationJob(
        OmniFlowDbContext dbContext,
        IPublishEndpoint publishEndpoint,
        ILogger<SlaReconciliationJob> logger)
    {
        _dbContext = dbContext;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var now = DateTimeOffset.UtcNow;

        // Cross-tenant system sweep: IgnoreQueryFilters is intentional here —
        // the job watches every tenant's SLAs. The event carries each ticket's
        // own TenantId so the consumer re-scopes correctly.
        var overdue = await _dbContext.Tickets
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted
                        && t.SlaDueDate != null
                        && t.SlaDueDate < now
                        && t.SlaBreachNotifiedAt == null
                        && t.CurrentState != TicketState.Resolved
                        && t.CurrentState != TicketState.Closed)
            .Select(t => new { t.Id, t.TenantId, t.SlaDueDate })
            .Take(100)
            .ToListAsync(context.CancellationToken);

        if (overdue.Count == 0) return;

        _logger.LogInformation("SLA reconciliation found {Count} overdue ticket(s).", overdue.Count);

        foreach (var ticket in overdue)
        {
            await _publishEndpoint.Publish(new CheckTicketSlaBreachEvent
            {
                TenantId = ticket.TenantId,
                TicketId = ticket.Id,
                ExpectedSlaResolutionTime = ticket.SlaDueDate!.Value.UtcDateTime
            }, context.CancellationToken);
        }

        // With the bus outbox, published messages sit in the outbox table until
        // this SaveChanges commits them.
        await _dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
