using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class KnowledgeBaseArticle : AuditableEntity
{
    
    public MultiLingualString LocalizedTitle { get; set; } = new();
    public MultiLingualString LocalizedContent { get; set; } = new();
    
    public Guid DepartmentId { get; set; }
    public Guid? CategoryId { get; set; }
    
    // Metadata
    public bool IsPublished { get; set; }
    public int ViewCount { get; set; }
    
    // Navigation
    public Department Department { get; set; } = null!;
    public TicketCategory? Category { get; set; }
}
