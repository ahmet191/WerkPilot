using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class AuthorityDelegation : AuditableEntity
{
    public Guid DelegatorId { get; set; }
    public Guid DelegateeId { get; set; }
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    
    // Navigation
    public User Delegator { get; set; } = null!;
    public User Delegatee { get; set; } = null!;
}
