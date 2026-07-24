using MediatR;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Events;

public record TicketStateTransitionedDomainEvent(
    Ticket Ticket,
    TicketState PreviousState,
    TicketState NewState,
    string Trigger) : INotification;
