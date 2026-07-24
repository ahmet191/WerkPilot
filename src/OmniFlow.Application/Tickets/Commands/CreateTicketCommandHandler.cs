using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Events;

namespace OmniFlow.Application.Tickets.Commands;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IMediator _mediator;
    private readonly ITenantService _tenantService;
    private readonly MassTransit.IMessageScheduler _messageScheduler;

    public CreateTicketCommandHandler(
        IOmniFlowDbContext context,
        IMediator mediator,
        ITenantService tenantService,
        MassTransit.IMessageScheduler messageScheduler)
    {
        _context = context;
        _mediator = mediator;
        _tenantService = tenantService;
        _messageScheduler = messageScheduler;
    }

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken)
            ?? throw new NotFoundException(nameof(TicketTemplate), request.TemplateId);

        var requesterExists = await _context.Set<User>()
            .AnyAsync(u => u.Id == request.RequesterId, cancellationToken);
        if (!requesterExists)
        {
            throw new NotFoundException(nameof(User), request.RequesterId);
        }

        // Tenant SLA policy: the strictest configured resolution target for the
        // submitted priority becomes the ticket's deadline. No config → no SLA.
        TimeSpan? slaResolutionTime = null;
        if (request.DynamicData.TryGetValue("Priority", out var priorityValue))
        {
            var priority = priorityValue?.ToString();
            var resolutionMinutes = await _context.SLAConfigurations
                .Where(s => s.PriorityLevel == priority)
                .Select(s => (int?)s.ResolutionTimeMinutes)
                .MinAsync(cancellationToken);
            if (resolutionMinutes.HasValue)
            {
                slaResolutionTime = TimeSpan.FromMinutes(resolutionMinutes.Value);
            }
        }

        // The aggregate validates the dynamic fields and produces the Create audit.
        var ticket = Ticket.Create(template, request.RequesterId, request.DynamicData, slaResolutionTime);
        _context.Tickets.Add(ticket);

        // Schedule the SLA breach check BEFORE SaveChanges: the scheduler
        // publishes through the transactional outbox, so the schedule commits
        // atomically with the ticket and lands in the persistent Quartz store.
        if (ticket.SlaDueDate.HasValue)
        {
            await _messageScheduler.SchedulePublish(
                ticket.SlaDueDate.Value.UtcDateTime,
                new CheckTicketSlaBreachEvent
                {
                    TenantId = _tenantService.TenantId,
                    TicketId = ticket.Id,
                    ExpectedSlaResolutionTime = ticket.SlaDueDate.Value.UtcDateTime
                },
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TicketCreatedDomainEvent(ticket), cancellationToken);

        return ticket.Id;
    }
}
