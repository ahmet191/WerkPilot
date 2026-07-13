using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Queries;

public class TicketDetailsDto : TicketDto
{
    public string Description { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public Dictionary<string, object> DynamicData { get; set; } = new();

    /// <summary>Triggers available from the current state (drives transition buttons).</summary>
    public List<string> AvailableTriggers { get; set; } = new();

    public List<TicketCommentDto> Comments { get; set; } = new();
    public List<TicketHistoryDto> History { get; set; } = new();
}

public class TicketCommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class TicketHistoryDto
{
    public string Action { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; }
}

public record GetTicketDetailsQuery(Guid TicketId) : IRequest<TicketDetailsDto>;

public class GetTicketDetailsQueryHandler : IRequestHandler<GetTicketDetailsQuery, TicketDetailsDto>
{
    private readonly IOmniFlowDbContext _context;

    public GetTicketDetailsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TicketDetailsDto> Handle(GetTicketDetailsQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Template)
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.AuditLogs).ThenInclude(a => a.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
            throw new NotFoundException(nameof(Ticket), request.TicketId);

        return new TicketDetailsDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            Description = ticket.Description,
            Type = ticket.Type.ToString(),
            Priority = ticket.Priority.ToString(),
            Impact = ticket.Impact.ToString(),
            Urgency = ticket.Urgency.ToString(),
            CurrentState = ticket.CurrentState.ToString(),
            RequesterId = ticket.RequesterId,
            RequesterName = ticket.Requester?.Name ?? string.Empty,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.Name ?? string.Empty,
            SlaDueDate = ticket.SlaDueDate,
            CreatedAt = ticket.CreatedAt,
            TemplateId = ticket.TemplateId,
            TemplateName = ticket.Template?.LocalizedName.GetValueOrDefault("en") ?? string.Empty,
            DynamicData = ticket.DynamicData,
            AvailableTriggers = ticket.Template?.Workflow?.Transitions?
                .Where(tr => tr.FromState == ticket.CurrentState)
                .Select(tr => tr.Trigger)
                .Distinct()
                .ToList() ?? new List<string>(),
            Comments = ticket.Comments
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TicketCommentDto
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author?.Name ?? string.Empty,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                }).ToList(),
            History = ticket.AuditLogs
                .OrderByDescending(a => a.ChangedAt)
                .Select(a => new TicketHistoryDto
                {
                    Action = a.Action,
                    FieldName = a.FieldName,
                    OldValue = a.OldValue,
                    NewValue = a.NewValue,
                    ChangedByName = a.ChangedBy?.Name ?? string.Empty,
                    ChangedAt = a.ChangedAt
                }).ToList()
        };
    }
}
