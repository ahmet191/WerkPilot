using MediatR;

namespace OmniFlow.Application.Tickets.Queries;

/// <summary>Slim list-item projection — entities/navigations never leave the API.</summary>
public class TicketDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string Urgency { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public string AssigneeName { get; set; } = string.Empty;
    public DateTimeOffset? SlaDueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public record GetTicketsQuery(Guid RequesterId) : IRequest<List<TicketDto>>;

public record GetTicketsByServiceDeskQuery(Guid ServiceDeskId) : IRequest<List<TicketDto>>;
