using OmniFlow.Domain.Common;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Domain.Entities;

/// <summary>
/// An in-app notification addressed to a single user. Persisted for every
/// notifiable event so the notification centre has history even when the
/// outbound channels (email, Telegram) are unavailable or unconfigured.
/// </summary>
public class Notification : AuditableEntity
{
    public Guid RecipientUserId { get; set; }
    public NotificationKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>Relative SPA route the notification deep-links to (e.g. "/approvals").</summary>
    public string? Link { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public bool IsRead => ReadAt.HasValue;

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }
}
