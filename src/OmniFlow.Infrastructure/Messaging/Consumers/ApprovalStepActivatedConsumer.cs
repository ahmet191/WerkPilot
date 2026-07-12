using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Notifies the approver of a newly activated approval step via Telegram
/// (when they have a linked chat). Role-addressed steps have no single
/// recipient and surface through the "My Approvals" screen instead.
/// </summary>
public class ApprovalStepActivatedConsumer : IConsumer<ApprovalStepActivatedEvent>
{
    private readonly IOmniFlowDbContext _dbContext;
    private readonly IMessageGateway _messageGateway;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ApprovalStepActivatedConsumer> _logger;

    public ApprovalStepActivatedConsumer(
        IOmniFlowDbContext dbContext,
        IMessageGateway messageGateway,
        ITenantService tenantService,
        ILogger<ApprovalStepActivatedConsumer> logger)
    {
        _dbContext = dbContext;
        _messageGateway = messageGateway;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApprovalStepActivatedEvent> context)
    {
        var evt = context.Message;
        _tenantService.SetTenantId(evt.TenantId);

        if (!evt.ApproverUserId.HasValue)
        {
            _logger.LogInformation(
                "Approval step {Step} of request {RequestId} targets role '{Role}'; no direct notification.",
                evt.StepOrder, evt.ApprovalRequestId, evt.ApproverRole);
            return;
        }

        var approver = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == evt.ApproverUserId.Value, context.CancellationToken);

        if (approver is null || string.IsNullOrEmpty(approver.TelegramChatId)) return;

        var message =
            $"Approval needed ✋\n{evt.EntityType}: {evt.EntityDisplay}\nStep {evt.StepOrder} is waiting for your decision.";

        await _messageGateway.SendMessageAsync(approver.TelegramChatId, message, context.CancellationToken);
    }
}
