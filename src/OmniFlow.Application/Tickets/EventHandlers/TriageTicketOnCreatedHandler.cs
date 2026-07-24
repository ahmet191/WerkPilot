using MediatR;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Events;

namespace OmniFlow.Application.Tickets.EventHandlers;

/// <summary>
/// Runs the AI triage model against a newly created ticket and stores its
/// prediction (category, priority, confidence, reasoning) as an advisory record.
/// Triage never blocks ticket creation: any model or persistence failure is
/// logged and swallowed, and the service itself falls back to a heuristic when
/// no Gemini key is configured. TenantId is stamped by the auditable interceptor
/// from the request's tenant, so predictions stay tenant-scoped.
/// </summary>
public class TriageTicketOnCreatedHandler : INotificationHandler<TicketCreatedDomainEvent>
{
    private readonly IAiTriageService _aiTriage;
    private readonly IOmniFlowDbContext _context;
    private readonly ILogger<TriageTicketOnCreatedHandler> _logger;

    public TriageTicketOnCreatedHandler(
        IAiTriageService aiTriage,
        IOmniFlowDbContext context,
        ILogger<TriageTicketOnCreatedHandler> logger)
    {
        _aiTriage = aiTriage;
        _context = context;
        _logger = logger;
    }

    public async Task Handle(TicketCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var ticket = notification.Ticket;
        try
        {
            var prediction = await _aiTriage.PredictTicketAsync(ticket.Title, ticket.Description, cancellationToken);
            prediction.TicketId = ticket.Id;
            prediction.TenantId = ticket.TenantId;

            _context.TicketAiPredictions.Add(prediction);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "AI triage for ticket {TicketId}: category={Category}, priority={Priority}, confidence={Confidence:P0}.",
                ticket.Id, prediction.PredictedCategory, prediction.PredictedPriority, prediction.ConfidenceScore);
        }
        catch (Exception ex)
        {
            // Advisory only — a triage failure must never fail ticket creation.
            _logger.LogError(ex, "AI triage failed for ticket {TicketId}; continuing without a prediction.", ticket.Id);
        }
    }
}
