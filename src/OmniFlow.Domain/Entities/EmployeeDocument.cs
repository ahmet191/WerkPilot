using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class EmployeeDocument : AuditableEntity
{
    public Guid UserId { get; set; }
    
    // Using MultiLingualString for document types like "Identity Card" or "Employment Contract"
    public MultiLingualString DocumentType { get; set; } = new();
    
    public string FileUrl { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    
    // Navigation
    public User User { get; set; } = null!;
}
