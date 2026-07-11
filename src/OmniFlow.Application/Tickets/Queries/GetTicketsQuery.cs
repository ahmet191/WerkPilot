using MediatR;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Tickets.Queries;

public record GetTicketsQuery(Guid RequesterId) : IRequest<List<Ticket>>;
