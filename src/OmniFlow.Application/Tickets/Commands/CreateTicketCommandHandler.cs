using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Domain.Events;
using Stateless;
using System.Text.RegularExpressions;

namespace OmniFlow.Application.Tickets.Commands;

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IMediator _mediator;
    private readonly MassTransit.IMessageScheduler _messageScheduler;

    public CreateTicketCommandHandler(IOmniFlowDbContext context, IMediator mediator, MassTransit.IMessageScheduler messageScheduler)
    {
        _context = context;
        _mediator = mediator;
        _messageScheduler = messageScheduler;
    }

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var template = await _context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, cancellationToken);

        if (template == null)
        {
            throw new Exception($"Template with ID {request.TemplateId} not found.");
        }

        var requester = await _context.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == request.RequesterId, cancellationToken);

        if (requester == null)
        {
            throw new Exception($"Requester with ID {request.RequesterId} not found.");
        }

        // 1. Perform Dynamic Field Validations (Regex, Dropdown options, Presence)
        foreach (var field in template.Fields)
        {
            var hasValue = request.DynamicData.TryGetValue(field.Key, out var fieldValue);
            
            if (field.IsRequired && (!hasValue || fieldValue == null || string.IsNullOrWhiteSpace(fieldValue.ToString())))
            {
                throw new Exception($"Required custom field '{field.Key}' is missing or empty.");
            }

            if (hasValue && fieldValue != null)
            {
                var stringVal = fieldValue.ToString() ?? string.Empty;

                // Validate dropdown choices
                if (field.DropdownOptions != null && field.DropdownOptions.Count > 0)
                {
                    if (!field.DropdownOptions.Contains(stringVal))
                    {
                        throw new Exception($"Custom field '{field.Key}' value '{stringVal}' is invalid. Allowed options: {string.Join(", ", field.DropdownOptions)}");
                    }
                }

                // Validate Regex rules
                if (!string.IsNullOrEmpty(field.ValidationRegex))
                {
                    if (!Regex.IsMatch(stringVal, field.ValidationRegex))
                    {
                        throw new Exception($"Custom field '{field.Key}' value '{stringVal}' does not match validation pattern: '{field.ValidationRegex}'");
                    }
                }
            }
        }

        // 2. Set up initial Ticket State
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            TemplateId = request.TemplateId,
            RequesterId = request.RequesterId,
            CurrentState = TicketState.New,
            DynamicData = request.DynamicData,
            
            // Extract standard fields from dynamic data if present, or set placeholders
            Title = request.DynamicData.TryGetValue("Title", out var titleObj) ? titleObj.ToString() ?? "Untitled Ticket" : "Untitled Ticket",
            Description = request.DynamicData.TryGetValue("Description", out var descObj) ? descObj.ToString() ?? string.Empty : string.Empty
        };

        // 3. Write initial Audit Log
        var audit = new TicketAuditLog
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            ChangedById = request.RequesterId,
            ChangedAt = DateTimeOffset.UtcNow,
            Action = "Create",
            FieldName = "Ticket",
            NewValue = $"Created ticket with Template: {template.LocalizedName.GetValueOrDefault("en")}"
        };

        _context.Tickets.Add(ticket);
        _context.Set<TicketAuditLog>().Add(audit);

        await _context.SaveChangesAsync(cancellationToken);

        // 4. Publish Domain Event
        await _mediator.Publish(new TicketCreatedDomainEvent(ticket), cancellationToken);

        // 5. Schedule SLA Breach Check
        if (ticket.SlaDueDate.HasValue)
        {
            await _messageScheduler.SchedulePublish(
                ticket.SlaDueDate.Value.UtcDateTime, 
                new OmniFlow.Domain.Events.CheckTicketSlaBreachEvent
                {
                    TicketId = ticket.Id,
                    ExpectedSlaResolutionTime = ticket.SlaDueDate.Value.UtcDateTime
                }, 
                cancellationToken);
        }

        return ticket.Id;
    }
}
