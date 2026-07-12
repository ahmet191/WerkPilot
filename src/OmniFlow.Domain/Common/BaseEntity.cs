using System.ComponentModel.DataAnnotations.Schema;
using MediatR;

namespace OmniFlow.Domain.Common;

/// <summary>
/// Root of every persisted entity: a Guid primary key plus an in-memory
/// domain-event collection (dispatched around SaveChanges; never mapped).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    private readonly List<INotification> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(INotification domainEvent) => _domainEvents.Add(domainEvent);

    public void RemoveDomainEvent(INotification domainEvent) => _domainEvents.Remove(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
