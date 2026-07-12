using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

namespace OmniFlow.Application.Approvals;

/// <summary>
/// Materializes an <see cref="ApprovalDefinition"/> template into a concrete
/// <see cref="ApprovalRequest"/> with resolved approvers. Adds the request to
/// the change tracker and publishes the first step's activation through the
/// outbox — the caller owns SaveChanges, so everything commits atomically
/// with the governed entity.
/// </summary>
public class ApprovalRequestBuilder
{
    private readonly IOmniFlowDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ITenantService _tenantService;

    public ApprovalRequestBuilder(
        IOmniFlowDbContext context,
        IPublishEndpoint publishEndpoint,
        ITenantService tenantService)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _tenantService = tenantService;
    }

    public async Task<ApprovalRequest> CreateAsync(
        string entityType,
        Guid entityId,
        string entityDisplay,
        Guid requestedById,
        ApprovalDefinition definition,
        CancellationToken cancellationToken)
    {
        if (definition.Steps.Count == 0)
        {
            throw new Common.Exceptions.ConflictException(
                $"The approval policy for {entityType} has no steps configured.");
        }

        var request = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplay = entityDisplay,
            RequestedById = requestedById,
            Status = ApprovalStatus.Pending,
            CurrentStepOrder = 1
        };

        var order = 1;
        foreach (var stepDefinition in definition.Steps.OrderBy(s => s.Order))
        {
            request.Steps.Add(await ResolveStepAsync(stepDefinition, order++, request.Id, requestedById, cancellationToken));
        }

        _context.ApprovalRequests.Add(request);

        var firstStep = request.Steps.First(s => s.Order == 1);
        await _publishEndpoint.Publish(new ApprovalStepActivatedEvent
        {
            TenantId = _tenantService.TenantId,
            ApprovalRequestId = request.Id,
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplay = entityDisplay,
            StepOrder = firstStep.Order,
            ApproverUserId = firstStep.ApproverUserId,
            ApproverRole = firstStep.ApproverRole
        }, cancellationToken);

        return request;
    }

    private async Task<ApprovalStep> ResolveStepAsync(
        ApprovalStepDefinition definition,
        int order,
        Guid requestId,
        Guid requestedById,
        CancellationToken cancellationToken)
    {
        var step = new ApprovalStep
        {
            Id = Guid.NewGuid(),
            ApprovalRequestId = requestId,
            Order = order,
            ApproverType = definition.ApproverType,
            ApproverRole = definition.ApproverRole,
            ApproverUserId = definition.ApproverUserId,
            Status = ApprovalStatus.Pending
        };

        if (definition.ApproverType == ApproverType.ManagerOfRequester)
        {
            var managerId = await _context.Users
                .Where(u => u.Id == requestedById)
                .Select(u => u.ManagerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (managerId.HasValue)
            {
                step.ApproverUserId = managerId;
            }
            else
            {
                // No manager on record: fall back to the HR admin role so the
                // chain can never dead-lock on an unresolvable approver.
                step.ApproverType = ApproverType.Role;
                step.ApproverRole = "HRAdmin";
            }
        }

        return step;
    }
}
