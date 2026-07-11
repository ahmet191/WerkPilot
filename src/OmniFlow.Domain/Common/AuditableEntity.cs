namespace OmniFlow.Domain.Common;

public abstract class AuditableEntity
{
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}
