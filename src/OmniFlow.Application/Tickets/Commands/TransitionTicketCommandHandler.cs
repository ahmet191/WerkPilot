using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Approvals;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;

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
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ticket), request.TicketId);

        var user = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.ChangedById, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.ChangedById);

        var previousState = ticket.CurrentState;

        // The aggregate enforces the transition rules (valid trigger, approval
        // guard, required role) and records the audit entry.
        var transition = ticket.Transition(request.Trigger, user, request.IsSystem);

        // Landing in WaitingForApproval opens an approval chain. The chain, the
        // state change and the activation event commit atomically (outbox).
        if (ticket.CurrentState == TicketState.WaitingForApproval)
        {
            var policy = transition.ApprovalPolicy ?? new ApprovalDefinition
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

        await _context.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TicketStateTransitionedDomainEvent(
            ticket, previousState, ticket.CurrentState, request.Trigger), cancellationToken);

        return true;
    }
}
