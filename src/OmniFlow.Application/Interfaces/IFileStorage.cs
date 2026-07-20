namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Binary blob store for uploaded files. The local implementation writes to
/// disk; the abstraction leaves room for S3/Azure Blob without touching callers.
/// Keys are opaque — callers persist the returned key and never construct one.
/// </summary>
public interface IFileStorage
{
    /// <summary>Persists a stream under the given tenant and returns its storage key.</summary>
    Task<string> SaveAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens the stored bytes for reading, or null if the key is unknown.</summary>
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}
