using MassTransit;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Tells the approver a step is waiting on them. Role-addressed steps fan out
/// to every holder of that role — they previously produced no notification at
/// all and relied on someone happening to open "My Approvals".
/// </summary>
public class ApprovalStepActivatedConsumer : IConsumer<ApprovalStepActivatedEvent>
{
    private readonly INotificationService _notifications;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ApprovalStepActivatedConsumer> _logger;

    public ApprovalStepActivatedConsumer(
        INotificationService notifications,
        ITenantService tenantService,
        ILogger<ApprovalStepActivatedConsumer> logger)
    {
        _notifications = notifications;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApprovalStepActivatedEvent> context)
    {
        var evt = context.Message;
        _tenantService.SetTenantId(evt.TenantId);

        var title = "Approval needed";
        var body = $"{evt.EntityType}: {evt.EntityDisplay} — step {evt.StepOrder} is waiting for your decision.";
        const string link = "/approvals";

        if (evt.ApproverUserId.HasValue)
        {
            await _notifications.NotifyUserAsync(
                evt.ApproverUserId.Value, NotificationKind.ApprovalRequested,
                title, body, link, context.CancellationToken);
            return;
        }

        if (!string.IsNullOrWhiteSpace(evt.ApproverRole))
        {
            await _notifications.NotifyRoleAsync(
                evt.ApproverRole, NotificationKind.ApprovalRequested,
                title, body, link, context.CancellationToken);
            return;
        }

        _logger.LogWarning(
            "Approval step {Step} of request {RequestId} has neither an approver nor a role; nobody notified.",
            evt.StepOrder, evt.ApprovalRequestId);
    }
}
