using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Infrastructure.Storage;

/// <summary>Binds the "Storage:S3" config section. An empty ServiceUrl targets
/// real AWS by Region; a set ServiceUrl (with ForcePathStyle) targets any
/// S3-compatible endpoint such as MinIO.</summary>
public class S3StorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string BucketName { get; set; } = "werkpilot-uploads";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    /// <summary>MinIO and most self-hosted gateways require path-style addressing.</summary>
    public bool ForcePathStyle { get; set; } = true;
}

/// <summary>
/// S3-compatible blob store. Mirrors <see cref="LocalFileStorage"/>'s contract
/// exactly — the same server-generated <c>&lt;tenant&gt;/&lt;guid&gt;&lt;ext&gt;</c>
/// key layout — so callers are identical whichever provider is configured.
/// Keys are opaque object names, so path traversal is a non-issue by construction.
/// </summary>
public class S3FileStorage : IFileStorage
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<S3FileStorage> _logger;

    // The bucket is ensured once, lazily, on the first write — real AWS usually
    // has it pre-provisioned, but MinIO in local dev starts empty.
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _bucketReady;

    public S3FileStorage(IAmazonS3 s3, S3StorageOptions options, ILogger<S3FileStorage> logger)
    {
        _s3 = s3;
        _bucket = options.BucketName;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Guid tenantId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        // Identical key shape to LocalFileStorage: the extension is cosmetic, the
        // GUID makes the key unguessable, and the original name lives in the DB.
        var extension = Path.GetExtension(fileName);
        if (extension.Length > 16) extension = string.Empty;
        var key = $"{tenantId}/{Guid.NewGuid()}{extension}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            AutoCloseStream = false,
        }, cancellationToken);

        return key;
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, storageKey, cancellationToken);
            // Buffer into a seekable stream so the S3 response can be disposed
            // before the caller reads — the caller owns the returned stream.
            var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;
            return buffer;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Storage key {StorageKey} has no backing object.", storageKey);
            return null;
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_bucket, storageKey, cancellationToken);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady) return;
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady) return;
            if (!await AmazonS3Util.DoesS3BucketExistV2Async(_s3, _bucket))
            {
                await _s3.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, cancellationToken);
                _logger.LogInformation("Created storage bucket {Bucket}.", _bucket);
            }
            _bucketReady = true;
        }
        finally
        {
            _initLock.Release();
        }
    }
}
