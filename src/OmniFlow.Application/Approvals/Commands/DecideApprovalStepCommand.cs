using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Events;

namespace OmniFlow.Application.Approvals.Commands;

/// <summary>
/// Approves or rejects the CURRENT step of a pending approval request.
/// Only the resolved approver (matching user, holder of the configured role,
/// an active delegate of the approver, or an administrator) may decide.
/// </summary>
public record DecideApprovalStepCommand(Guid ApprovalRequestId, bool Approve, string? Comment) : IRequest<bool>;

public class DecideApprovalStepCommandValidator : AbstractValidator<DecideApprovalStepCommand>
{
    public DecideApprovalStepCommandValidator()
    {
        RuleFor(x => x.ApprovalRequestId).NotEmpty();
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class DecideApprovalStepCommandHandler : IRequestHandler<DecideApprovalStepCommand, bool>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ITenantService _tenantService;
    private readonly IPublishEndpoint _publishEndpoint;

    public DecideApprovalStepCommandHandler(
        IOmniFlowDbContext context,
        IUserContext userContext,
        ITenantService tenantService,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _userContext = userContext;
        _tenantService = tenantService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(DecideApprovalStepCommand request, CancellationToken cancellationToken)
    {
        var approval = await _context.ApprovalRequests
            .Include(a => a.Steps)
            .FirstOrDefaultAsync(a => a.Id == request.ApprovalRequestId, cancellationToken)
            ?? throw new NotFoundException("ApprovalRequest", request.ApprovalRequestId);

        // The aggregate enforces the pending/current-step invariants.
        var step = approval.CurrentStep();

        // Authorization: which of my delegators granted me authority right now?
        var userId = _userContext.UserId;
        var now = DateTime.UtcNow;
        var delegatedApproverIds = await _context.AuthorityDelegations
            .Where(d => d.DelegateeId == userId && d.StartDate <= now && d.EndDate >= now)
            .Select(d => d.DelegatorId)
            .ToListAsync(cancellationToken);

        if (!ApprovalRequest.CanDecide(step, userId, _userContext.Role, delegatedApproverIds))
        {
            throw new ForbiddenAccessException("You are not the approver for the current step of this request.");
        }

        var outcome = approval.Decide(userId, request.Approve, request.Comment);

        if (outcome.Kind == ApprovalDecisionKind.Completed)
        {
            await _publishEndpoint.Publish(new ApprovalCompletedEvent
            {
                TenantId = _tenantService.TenantId,
                ApprovalRequestId = approval.Id,
                EntityType = approval.EntityType,
                EntityId = approval.EntityId,
                IsApproved = outcome.IsApproved,
                DecidedById = userId,
                Comment = request.Comment
            }, cancellationToken);
        }
        else
        {
            var next = outcome.NextStep!;
            await _publishEndpoint.Publish(new ApprovalStepActivatedEvent
            {
                TenantId = _tenantService.TenantId,
                ApprovalRequestId = approval.Id,
                EntityType = approval.EntityType,
                EntityId = approval.EntityId,
                EntityDisplay = approval.EntityDisplay,
                StepOrder = next.Order,
                ApproverUserId = next.ApproverUserId,
                ApproverRole = next.ApproverRole
            }, cancellationToken);
        }

        // Outbox: the decision and its events commit atomically.
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
