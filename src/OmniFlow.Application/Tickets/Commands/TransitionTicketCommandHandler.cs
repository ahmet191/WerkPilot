using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Approvals;
using OmniFlow.Application.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;
using Stateless;

namespace OmniFlow.Application.Tickets.Commands;

public class TransitionTicketCommandHandler : IRequestHandler<TransitionTicketCommand, bool>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IMediator _mediator;
    private readonly ApprovalRequestBuilder _approvalBuilder;

    public TransitionTicketCommandHandler(
        IOmniFlowDbContext context,
        IMediator mediator,
        ApprovalRequestBuilder approvalBuilder)
    {
        _context = context;
        _mediator = mediator;
        _approvalBuilder = approvalBuilder;
    }

    public async Task<bool> Handle(TransitionTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException(nameof(Ticket), request.TicketId);
        }

        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.ChangedById, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException(nameof(User), request.ChangedById);
        }

        var previousState = ticket.CurrentState;

        // 1. Configure Stateless State Machine dynamically based on Template's Workflow JSON column
        var stateMachine = new StateMachine<TicketState, string>(
            () => ticket.CurrentState,
            s => ticket.CurrentState = s);

        if (ticket.Template.Workflow?.Transitions == null || ticket.Template.Workflow.Transitions.Count == 0)
        {
            throw new ConflictException("The ticket template has no workflow transitions configured.");
        }

        // Configure all dynamic transitions
        foreach (var transition in ticket.Template.Workflow.Transitions)
        {
            stateMachine.Configure(transition.FromState)
                .Permit(transition.Trigger, transition.ToState);
        }

        // 2. Validate Role-Based Authorization for the transition
        var matchingTransition = ticket.Template.Workflow.Transitions
            .FirstOrDefault(t => t.FromState == previousState && t.Trigger == request.Trigger);

        if (matchingTransition == null)
        {
            throw new ConflictException($"Transition '{request.Trigger}' is not configured from state '{previousState}'.");
        }

        // Tickets waiting for approval are decided exclusively through the
        // approval engine; the outcome consumer fires the transition system-side.
        if (previousState == TicketState.WaitingForApproval && !request.IsSystem)
        {
            throw new ForbiddenAccessException(
                "This ticket is pending approval. Decisions are made from the Approvals screen, not by direct transition.");
        }

        if (!request.IsSystem &&
            matchingTransition.RequiredRole.HasValue &&
            matchingTransition.RequiredRole.Value != user.Role &&
            user.Role != UserRole.Administrator)
        {
            throw new ForbiddenAccessException($"Trigger '{request.Trigger}' requires the '{matchingTransition.RequiredRole}' role. Your role is '{user.Role}'.");
        }

        // 3. Fire state machine transition
        await stateMachine.FireAsync(request.Trigger);

        // 3b. Landing in WaitingForApproval opens an approval chain. The chain,
        // the state change and the activation event commit atomically (outbox).
        if (ticket.CurrentState == TicketState.WaitingForApproval)
        {
            var policy = matchingTransition.ApprovalPolicy ?? new ApprovalDefinition
            {
                // Sensible default when the template author didn't configure one.
                Steps = { new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.Role, ApproverRole = "Administrator" } }
            };

            await _approvalBuilder.CreateAsync(
                ApprovalEntityTypes.Ticket,
                ticket.Id,
                ticket.Title,
                request.ChangedById,
                policy,
                cancellationToken);
        }

        // 4. Record Audit Log entry
        var audit = new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = user.Id,
            ChangedAt = DateTimeOffset.UtcNow,
            Action = "Transition",
            FieldName = "CurrentState",
            OldValue = previousState.ToString(),
            NewValue = ticket.CurrentState.ToString()
        };

        _context.Set<TicketAuditLog>().Add(audit);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. Publish Domain Event
        await _mediator.Publish(new TicketStateTransitionedDomainEvent(
            ticket,
            previousState,
            ticket.CurrentState,
            request.Trigger), cancellationToken);

        return true;
    }
}
