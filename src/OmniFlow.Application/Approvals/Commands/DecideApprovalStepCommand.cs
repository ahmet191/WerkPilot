using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;
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
            .FirstOrDefaultAsync(a => a.Id == request.ApprovalRequestId, cancellationToken);

        if (approval is null)
        {
            throw new NotFoundException("ApprovalRequest", request.ApprovalRequestId);
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new ConflictException($"This approval request has already been {approval.Status}.");
        }

        var step = approval.Steps.FirstOrDefault(s => s.Order == approval.CurrentStepOrder && s.Status == ApprovalStatus.Pending);
        if (step is null)
        {
            throw new ConflictException("The approval request has no pending step to decide.");
        }

        await EnsureUserMayDecideAsync(step, cancellationToken);

        var userId = _userContext.UserId;
        step.Status = request.Approve ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        step.ActedById = userId;
        step.ActedAt = DateTimeOffset.UtcNow;
        step.Comment = request.Comment;

        if (!request.Approve)
        {
            // First rejection concludes the whole chain.
            approval.Status = ApprovalStatus.Rejected;
            approval.CompletedAt = DateTimeOffset.UtcNow;
            foreach (var remaining in approval.Steps.Where(s => s.Status == ApprovalStatus.Pending))
            {
                remaining.Status = ApprovalStatus.Cancelled;
            }

            await PublishCompletedAsync(approval.Id, approval.EntityType, approval.EntityId, false, userId, request.Comment, cancellationToken);
        }
        else
        {
            var nextStep = approval.Steps
                .Where(s => s.Status == ApprovalStatus.Pending)
                .OrderBy(s => s.Order)
                .FirstOrDefault();

            if (nextStep is null)
            {
                approval.Status = ApprovalStatus.Approved;
                approval.CompletedAt = DateTimeOffset.UtcNow;
                await PublishCompletedAsync(approval.Id, approval.EntityType, approval.EntityId, true, userId, request.Comment, cancellationToken);
            }
            else
            {
                approval.CurrentStepOrder = nextStep.Order;
                await _publishEndpoint.Publish(new ApprovalStepActivatedEvent
                {
                    TenantId = _tenantService.TenantId,
                    ApprovalRequestId = approval.Id,
                    EntityType = approval.EntityType,
                    EntityId = approval.EntityId,
                    EntityDisplay = approval.EntityDisplay,
                    StepOrder = nextStep.Order,
                    ApproverUserId = nextStep.ApproverUserId,
                    ApproverRole = nextStep.ApproverRole
                }, cancellationToken);
            }
        }

        // Outbox: the decision and its events commit atomically.
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureUserMayDecideAsync(Domain.Entities.Approvals.ApprovalStep step, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        var role = _userContext.Role;

        if (role == "Administrator") return;
        if (step.ApproverUserId == userId) return;
        if (step.ApproverRole is not null && step.ApproverRole == role) return;

        // Active delegation: the resolved approver delegated their authority.
        if (step.ApproverUserId.HasValue)
        {
            var now = DateTime.UtcNow;
            var delegated = await _context.AuthorityDelegations.AnyAsync(d =>
                d.DelegatorId == step.ApproverUserId.Value &&
                d.DelegateeId == userId &&
                d.StartDate <= now &&
                d.EndDate >= now, cancellationToken);

            if (delegated) return;
        }

        throw new ForbiddenAccessException("You are not the approver for the current step of this request.");
    }

    private Task PublishCompletedAsync(
        Guid approvalRequestId, string entityType, Guid entityId,
        bool isApproved, Guid decidedById, string? comment,
        CancellationToken cancellationToken) =>
        _publishEndpoint.Publish(new ApprovalCompletedEvent
        {
            TenantId = _tenantService.TenantId,
            ApprovalRequestId = approvalRequestId,
            EntityType = entityType,
            EntityId = entityId,
            IsApproved = isApproved,
            DecidedById = decidedById,
            Comment = comment
        }, cancellationToken);
}
