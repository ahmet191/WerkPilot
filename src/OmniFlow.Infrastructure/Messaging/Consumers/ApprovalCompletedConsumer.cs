using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Tickets.Commands;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

namespace OmniFlow.Infrastructure.Messaging.Consumers;

/// <summary>
/// Applies a concluded approval chain to the governed entity:
/// tickets get their Approve/Reject workflow trigger fired system-side,
/// leave requests get their status set (and the notification event raised).
/// </summary>
public class ApprovalCompletedConsumer : IConsumer<ApprovalCompletedEvent>
{
    /// <summary>Convention: transitions out of WaitingForApproval use these triggers.</summary>
    private const string ApproveTrigger = "Approve";
    private const string RejectTrigger = "Reject";

    private readonly IOmniFlowDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly ITenantService _tenantService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<ApprovalCompletedConsumer> _logger;

    public ApprovalCompletedConsumer(
        IOmniFlowDbContext dbContext,
        IMediator mediator,
        ITenantService tenantService,
        IPublishEndpoint publishEndpoint,
        ILogger<ApprovalCompletedConsumer> logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;
        _tenantService = tenantService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ApprovalCompletedEvent> context)
    {
        var evt = context.Message;
        // No HttpContext here: scope the DbContext to the event's tenant.
        _tenantService.SetTenantId(evt.TenantId);

        _logger.LogInformation(
            "Applying approval outcome {Outcome} to {EntityType} {EntityId}",
            evt.IsApproved ? "Approved" : "Rejected", evt.EntityType, evt.EntityId);

        switch (evt.EntityType)
        {
            case ApprovalEntityTypes.Ticket:
                await ApplyToTicketAsync(evt, context.CancellationToken);
                break;

            case ApprovalEntityTypes.LeaveRequest:
                await ApplyToLeaveRequestAsync(evt, context.CancellationToken);
                break;

            default:
                _logger.LogWarning("No outcome handler for approval entity type '{EntityType}'.", evt.EntityType);
                break;
        }
    }

    private async Task ApplyToTicketAsync(ApprovalCompletedEvent evt, CancellationToken cancellationToken)
    {
        // System-side transition: bypasses the WaitingForApproval guard and
        // role checks — authority was already established by the approval chain.
        await _mediator.Send(new TransitionTicketCommand(
            evt.EntityId,
            evt.IsApproved ? ApproveTrigger : RejectTrigger,
            evt.DecidedById,
            IsSystem: true), cancellationToken);
    }

    private async Task ApplyToLeaveRequestAsync(ApprovalCompletedEvent evt, CancellationToken cancellationToken)
    {
        var leave = await _dbContext.LeaveRequests
            .FirstOrDefaultAsync(l => l.Id == evt.EntityId, cancellationToken);

        if (leave is null)
        {
            _logger.LogWarning("LeaveRequest {LeaveRequestId} not found while applying approval outcome.", evt.EntityId);
            return;
        }

        leave.Resolve(evt.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected, evt.DecidedById);

        // Reuses the existing notification chain (Telegram to the requester).
        await _publishEndpoint.Publish(new LeaveRequestResolvedEvent
        {
            TenantId = evt.TenantId,
            LeaveRequestId = leave.Id,
            RequesterId = leave.UserId,
            ApproverId = evt.DecidedById,
            IsApproved = evt.IsApproved
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
