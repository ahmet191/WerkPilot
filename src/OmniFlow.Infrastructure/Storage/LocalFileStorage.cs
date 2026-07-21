using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Storage;

/// <summary>
/// Stores uploaded files on the local filesystem under a configured root
/// (Storage:Root, default "storage/uploads"), partitioned per tenant. Storage
/// keys are server-generated GUID paths, so a caller can never smuggle a
/// path-traversal segment in through a filename.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _root;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(IConfiguration configuration, ILogger<LocalFileStorage> logger)
    {
        _root = Path.GetFullPath(configuration["Storage:Root"] ?? "storage/uploads");
        _logger = logger;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        // Key = <tenant>/<guid><ext>. The extension is cosmetic; the GUID makes
        // the path unguessable and collision-free. The original name is stored
        // separately in the database, never used to build a path.
        var extension = Path.GetExtension(fileName);
        if (extension.Length > 16) extension = string.Empty; // reject absurd extensions
        var key = $"{tenantId}/{Guid.NewGuid()}{extension}";

        var fullPath = ResolveWithinRoot(key);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = File.Create(fullPath);
        await content.CopyToAsync(file, cancellationToken);

        return key;
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinRoot(storageKey);
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Storage key {StorageKey} has no backing file.", storageKey);
            return Task.FromResult<Stream?>(null);
        }
        return Task.FromResult<Stream?>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveWithinRoot(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Resolves a key to an absolute path and refuses anything that escapes the
    /// root — defence in depth even though keys are server-generated.
    /// </summary>
    private string ResolveWithinRoot(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_root, storageKey));
        if (!fullPath.StartsWith(_root, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Resolved storage path escapes the storage root.");
        }
        return fullPath;
    }
}
