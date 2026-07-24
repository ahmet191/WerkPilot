using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

public class LeaveRequestNotificationConsumer :
    IConsumer<LeaveRequestCreatedEvent>,
    IConsumer<LeaveRequestResolvedEvent>
{
    private readonly IOmniFlowDbContext _dbContext;
    private readonly IMessageGateway _messageGateway;
    private readonly ITenantService _tenantService;
    private readonly ILogger<LeaveRequestNotificationConsumer> _logger;

    public LeaveRequestNotificationConsumer(
        IOmniFlowDbContext dbContext,
        IMessageGateway messageGateway,
        ITenantService tenantService,
        ILogger<LeaveRequestNotificationConsumer> logger)
    {
        _dbContext = dbContext;
        _messageGateway = messageGateway;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LeaveRequestCreatedEvent> context)
    {
        var evt = context.Message;
        // No HttpContext here: scope the DbContext to the event's tenant, or
        // the global query filter would silently return zero rows.
        _tenantService.SetTenantId(evt.TenantId);

        _logger.LogInformation("Processing LeaveRequestCreatedEvent for LeaveRequestId: {LeaveRequestId}", evt.LeaveRequestId);

        // Fetch the Requester to get their name and their Manager's ChatId
        var requester = await _dbContext.Users
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Id == evt.RequesterId, context.CancellationToken);

        if (requester == null) return;

        var managerChatId = requester.Manager?.TelegramChatId;
        if (string.IsNullOrEmpty(managerChatId))
        {
            _logger.LogInformation(
                "Leave request {LeaveRequestId}: requester's manager has no linked Telegram chat; skipping notification.",
                evt.LeaveRequestId);
            return;
        }

        var msg = $"New Leave Request Pending!\nRequester: {requester.Name}\nType: {evt.Type}\nDates: {evt.StartDate:d} - {evt.EndDate:d}";
        await _messageGateway.SendMessageAsync(managerChatId, msg, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<LeaveRequestResolvedEvent> context)
    {
        var evt = context.Message;
        _tenantService.SetTenantId(evt.TenantId);

        _logger.LogInformation("Processing LeaveRequestResolvedEvent for LeaveRequestId: {LeaveRequestId}", evt.LeaveRequestId);

        var requester = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == evt.RequesterId, context.CancellationToken);
        if (requester == null || string.IsNullOrEmpty(requester.TelegramChatId)) return;

        var status = evt.IsApproved ? "APPROVED ✅" : "REJECTED ❌";
        var msg = $"Your Leave Request update:\nStatus: {status}\nLeaveRequestId: {evt.LeaveRequestId}";

        await _messageGateway.SendMessageAsync(requester.TelegramChatId, msg, context.CancellationToken);
    }
}
