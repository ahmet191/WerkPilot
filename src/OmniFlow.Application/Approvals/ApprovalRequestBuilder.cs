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
/// <see cref="ApprovalRequest"/> aggregate with resolved approvers. Adds the
/// request to the change tracker and publishes the first step's activation
/// through the outbox — the caller owns SaveChanges, so everything commits
/// atomically with the governed entity.
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
        var order = 1;
        var steps = new List<ApprovalStep>();
        foreach (var stepDefinition in definition.Steps.OrderBy(s => s.Order))
        {
            steps.Add(await ResolveStepAsync(stepDefinition, order++, requestedById, cancellationToken));
        }

        // The aggregate enforces "at least one step" and wires up the parent link.
        var request = ApprovalRequest.Create(entityType, entityId, entityDisplay, requestedById, steps);
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
        Guid requestedById,
        CancellationToken cancellationToken)
    {
        var step = ApprovalStep.Create(order, definition.ApproverType, definition.ApproverRole, definition.ApproverUserId);

        if (definition.ApproverType == ApproverType.ManagerOfRequester)
        {
            var managerId = await _context.Users
                .Where(u => u.Id == requestedById)
                .Select(u => u.ManagerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (managerId.HasValue)
            {
                // Resolve the manager into a concrete SpecificUser step.
                step = ApprovalStep.Create(order, ApproverType.SpecificUser, null, managerId);
            }
            else
            {
                // No manager on record: fall back to the HR admin role so the
                // chain can never dead-lock on an unresolvable approver.
                step.FallBackToRole("HRAdmin");
            }
        }

        return step;
    }
}
