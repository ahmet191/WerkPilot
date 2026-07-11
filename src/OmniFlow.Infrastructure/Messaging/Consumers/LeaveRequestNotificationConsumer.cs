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
    private readonly ILogger<LeaveRequestNotificationConsumer> _logger;

    public LeaveRequestNotificationConsumer(
        IOmniFlowDbContext dbContext,
        IMessageGateway messageGateway,
        ILogger<LeaveRequestNotificationConsumer> logger)
    {
        _dbContext = dbContext;
        _messageGateway = messageGateway;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LeaveRequestCreatedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing LeaveRequestCreatedEvent for LeaveRequestId: {LeaveRequestId}", evt.LeaveRequestId);

        // Fetch the Requester to get their name and their Manager's ChatId
        var requester = await _dbContext.Users
            .Include(u => u.Manager)
            .FirstOrDefaultAsync(u => u.Id == evt.RequesterId, context.CancellationToken);

        if (requester == null) return;

        // In a real system, the manager ID might be on the leave request or we fallback to the user's manager
        // Here we just notify the Approver or Manager if they have a Telegram Chat ID.
        // For testing, let's notify the Manager. If no Manager is assigned, we can notify a hardcoded admin/approver.
        
        var managerChatId = requester.Manager?.TelegramChatId ?? "8920226584"; // Defaulting to my chat ID for demo

        var msg = $"New Leave Request Pending!\nRequester: {requester.Name}\nType: {evt.Type}\nDates: {evt.StartDate:d} - {evt.EndDate:d}";
        await _messageGateway.SendMessageAsync(managerChatId, msg, context.CancellationToken);
    }

    public async Task Consume(ConsumeContext<LeaveRequestResolvedEvent> context)
    {
        var evt = context.Message;
        _logger.LogInformation("Processing LeaveRequestResolvedEvent for LeaveRequestId: {LeaveRequestId}", evt.LeaveRequestId);

        var requester = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == evt.RequesterId, context.CancellationToken);
        if (requester == null || string.IsNullOrEmpty(requester.TelegramChatId)) return;

        var status = evt.IsApproved ? "APPROVED ✅" : "REJECTED ❌";
        var msg = $"Your Leave Request update:\nStatus: {status}\nLeaveRequestId: {evt.LeaveRequestId}";
        
        await _messageGateway.SendMessageAsync(requester.TelegramChatId, msg, context.CancellationToken);
    }
}
