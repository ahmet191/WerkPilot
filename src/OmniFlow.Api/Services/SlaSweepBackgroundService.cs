using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Api.Services;

/// <summary>
/// Durable safety net for SLA breach detection. The in-memory message
/// scheduler loses its delayed messages on restart, so this service sweeps
/// for overdue unresolved tickets once a minute and publishes breach events.
/// The consumer's <c>SlaBreachNotifiedAt</c> guard keeps alerts idempotent
/// even when both mechanisms fire.
/// </summary>
public class SlaSweepBackgroundService : BackgroundService
{
    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaSweepBackgroundService> _logger;

    public SlaSweepBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SlaSweepBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(SweepInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SLA sweep failed; will retry on the next tick.");
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OmniFlowDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var now = DateTimeOffset.UtcNow;

        // Cross-tenant system sweep: IgnoreQueryFilters is intentional here —
        // the background service watches every tenant's SLAs. The event carries
        // each ticket's own TenantId so the consumer re-scopes correctly.
        var overdue = await dbContext.Tickets
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted
                        && t.SlaDueDate != null
                        && t.SlaDueDate < now
                        && t.SlaBreachNotifiedAt == null
                        && t.CurrentState != TicketState.Resolved
                        && t.CurrentState != TicketState.Closed)
            .Select(t => new { t.Id, t.TenantId, t.SlaDueDate })
            .Take(100)
            .ToListAsync(cancellationToken);

        if (overdue.Count == 0) return;

        _logger.LogInformation("SLA sweep found {Count} overdue ticket(s).", overdue.Count);

        foreach (var ticket in overdue)
        {
            await publishEndpoint.Publish(new CheckTicketSlaBreachEvent
            {
                TenantId = ticket.TenantId,
                TicketId = ticket.Id,
                ExpectedSlaResolutionTime = ticket.SlaDueDate!.Value.UtcDateTime
            }, cancellationToken);
        }

        // With the bus outbox, published messages sit in the outbox table until
        // this SaveChanges commits them.
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
