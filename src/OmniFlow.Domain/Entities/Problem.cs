using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

public class Problem : AuditableEntity
{
    public Guid Id { get; set; }
    public string ProblemNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ProblemState State { get; set; }
    public string? RootCauseDetails { get; set; }
    public string? Workaround { get; set; }
    
    public Guid? AssigneeId { get; set; }
    public Guid ServiceDeskId { get; set; }
    
    // Navigation
    public User? Assignee { get; set; }
    public ServiceDesk? ServiceDesk { get; set; }
    public ICollection<Ticket> Incidents { get; set; } = new List<Ticket>();
}
