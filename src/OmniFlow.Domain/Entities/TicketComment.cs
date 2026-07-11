using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class TicketComment : AuditableEntity
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    
    // Internal notes are only visible to Agents/Admins, not the Requester
    public bool IsInternal { get; set; }
    
    public User Author { get; set; } = null!;
}
