namespace OmniFlow.Domain.Common;

/// <summary>
/// Hard deletes are forbidden across the platform. Entities implementing this
/// interface are flipped to <see cref="IsDeleted"/> by the save interceptor
/// whenever code calls Remove(), and filtered out by the global query filter.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
    Guid? DeletedBy { get; set; }
}
