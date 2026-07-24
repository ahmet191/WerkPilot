namespace OmniFlow.Application.Interfaces;

/// <summary>
/// Outbound email channel. Implementations must be safe to call when email is
/// not configured (no-op + log) so callers never need to know whether SMTP
/// exists in the current environment.
/// </summary>
public interface IEmailSender
{
    bool Enabled { get; }
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
