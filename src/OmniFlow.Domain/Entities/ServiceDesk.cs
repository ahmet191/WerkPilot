using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class ServiceDesk : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid DepartmentId { get; set; } // The department that owns this service desk
    public Guid? AdminId { get; set; } // Service Desk Admin
    
    // Visibility / Access Control
    public bool IsPublicToAllEmployees { get; set; } = true;
    public List<Guid> AllowedDepartmentIds { get; set; } = new(); // If IsPublic is false, these are the allowed departments
    
    // Navigation
    public Department Department { get; set; } = null!;
    public User? Admin { get; set; }
    
    public ICollection<TicketTemplate> Templates { get; set; } = new List<TicketTemplate>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Problem> Problems { get; set; } = new List<Problem>();
    public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
}
