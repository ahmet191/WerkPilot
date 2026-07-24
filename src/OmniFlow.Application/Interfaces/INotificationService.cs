using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Interfaces;

/// <summary>Payload pushed to a connected client in real time.</summary>
public record NotificationPayload(
    Guid Id,
    NotificationKind Kind,
    string Title,
    string Body,
    string? Link,
    DateTimeOffset CreatedAt);

/// <summary>
/// Delivers a notification to connected clients. Implemented in the web layer
/// (SignalR); declared here so Infrastructure stays free of a web dependency.
/// </summary>
public interface INotificationPusher
{
    Task PushAsync(Guid recipientUserId, NotificationPayload payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Single entry point for user-facing notifications. Always records an in-app
/// notification, then fans out to the channels the recipient can actually
/// receive (email, Telegram) — no channel failure can break the caller.
/// </summary>
public interface INotificationService
{
    Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string body,
        string? link = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies every user holding a domain role. Role-addressed approval steps
    /// have no single recipient and used to go unnotified entirely.
    /// </summary>
    Task NotifyRoleAsync(
        string roleName,
        NotificationKind kind,
        string title,
        string body,
        string? link = null,
        CancellationToken cancellationToken = default);
}
