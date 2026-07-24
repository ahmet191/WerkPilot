using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

public class AssetAssignedConsumer : IConsumer<AssetAssignedEvent>
{
    private readonly ILogger<AssetAssignedConsumer> _logger;
    private readonly INotificationService _notifications;
    private readonly IOmniFlowDbContext _context;
    private readonly ITenantService _tenantService;

    public AssetAssignedConsumer(
        ILogger<AssetAssignedConsumer> logger,
        INotificationService notifications,
        IOmniFlowDbContext context,
        ITenantService tenantService)
    {
        _logger = logger;
        _notifications = notifications;
        _context = context;
        _tenantService = tenantService;
    }

    public async Task Consume(ConsumeContext<AssetAssignedEvent> context)
    {
        var ev = context.Message;
        // No HttpContext here: scope the DbContext to the event's tenant.
        _tenantService.SetTenantId(ev.TenantId);
        _logger.LogInformation("Processing AssetAssignedEvent for Asset {AssetId}", ev.AssetId);

        await _notifications.NotifyUserAsync(
            ev.AssignedToUserId,
            NotificationKind.AssetAssigned,
            "Asset assigned to you",
            $"{ev.Model} (tag {ev.AssetTag}) was assigned to you on {ev.AssignedAt:yyyy-MM-dd HH:mm}.",
            "/assets",
            context.CancellationToken);
    }
}
