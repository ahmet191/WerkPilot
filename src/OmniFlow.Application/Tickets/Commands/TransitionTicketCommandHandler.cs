using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;
using Stateless;

namespace OmniFlow.Application.Tickets.Commands;

public class TransitionTicketCommandHandler : IRequestHandler<TransitionTicketCommand, bool>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IMediator _mediator;

    public TransitionTicketCommandHandler(IOmniFlowDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<bool> Handle(TransitionTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Template)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new Exception($"Ticket with ID {request.TicketId} not found.");
        }

        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.ChangedById, cancellationToken);

        if (user == null)
        {
            throw new Exception($"User with ID {request.ChangedById} not found. Operation unauthorized.");
        }

        var previousState = ticket.CurrentState;

        // 1. Configure Stateless State Machine dynamically based on Template's Workflow JSON column
        var stateMachine = new StateMachine<TicketState, string>(
            () => ticket.CurrentState,
            s => ticket.CurrentState = s);

        if (ticket.Template.Workflow?.Transitions == null || ticket.Template.Workflow.Transitions.Count == 0)
        {
            throw new Exception("The ticket template has no workflow transitions configured.");
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
            throw new Exception($"Transition '{request.Trigger}' is not configured from state '{previousState}'.");
        }

        if (matchingTransition.RequiredRole.HasValue && matchingTransition.RequiredRole.Value != user.Role && user.Role != UserRole.Administrator)
        {
            throw new Exception($"Unauthorized: Trigger '{request.Trigger}' requires the '{matchingTransition.RequiredRole}' role. Your role is '{user.Role}'.");
        }

        // 3. Fire state machine transition
        await stateMachine.FireAsync(request.Trigger);

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
