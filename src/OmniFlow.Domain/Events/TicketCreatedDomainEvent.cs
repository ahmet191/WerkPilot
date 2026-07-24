using MediatR;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Domain.Events;

public record TicketCreatedDomainEvent(Ticket Ticket) : INotification;
