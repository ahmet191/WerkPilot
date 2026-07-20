using OmniFlow.Domain.Common;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// A file attached to a ticket. Only metadata lives in the database; the bytes
/// live in the configured file store, addressed by <see cref="StorageKey"/>.
/// </summary>
public class TicketAttachment : AuditableEntity
{
    public Guid TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }

    /// <summary>Opaque key the file store resolves to the stored bytes.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public Guid UploadedById { get; set; }
    public User? UploadedBy { get; set; }
}
