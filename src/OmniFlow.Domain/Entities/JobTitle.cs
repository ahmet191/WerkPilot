using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

public class JobTitle : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Level { get; set; }
    public string? Band { get; set; }
    public bool IsManagerial { get; set; }
}
