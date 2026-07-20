using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Identity;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Infrastructure.Notifications;

/// <summary>
/// Records every notification in-app, then best-effort fans out to email,
/// Telegram and connected clients. Outbound channels are wrapped individually:
/// an unreachable SMTP relay or Telegram outage must never fail the business
/// operation that triggered the notification.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly OmniFlowDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailSender _emailSender;
    private readonly IMessageGateway _messageGateway;
    private readonly INotificationPusher _pusher;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        OmniFlowDbContext dbContext,
        ITenantService tenantService,
        IEmailSender emailSender,
        IMessageGateway messageGateway,
        INotificationPusher pusher,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _emailSender = emailSender;
        _messageGateway = messageGateway;
        _pusher = pusher;
        _logger = logger;
    }

    public async Task NotifyUserAsync(
        Guid recipientUserId,
        NotificationKind kind,
        string title,
        string body,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        var recipient = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == recipientUserId, cancellationToken);

        if (recipient is null)
        {
            _logger.LogWarning("Notification '{Title}' dropped: user {UserId} not found in tenant {TenantId}.",
                title, recipientUserId, _tenantService.TenantId);
            return;
        }

        var notification = new Notification
        {
            TenantId = _tenantService.TenantId,
            RecipientUserId = recipientUserId,
            Kind = kind,
            Title = title,
            Body = body,
            Link = link
        };
        _dbContext.Set<Notification>().Add(notification);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await FanOutAsync(notification, recipient, cancellationToken);
    }

    public async Task NotifyRoleAsync(
        string roleName,
        NotificationKind kind,
        string title,
        string body,
        string? link = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantService.TenantId;

        // Identity roles are the ones approval steps address (ApproverRole is
        // matched against the role claim), so resolve holders through Identity
        // and scope them to the current tenant.
        // NB: OmniFlowDbContext.Users is the *domain* user set (it shadows
        // Identity's), so login accounts are reached through Set<ApplicationUser>.
        var recipientIds = await (
            from userRole in _dbContext.UserRoles
            join role in _dbContext.Roles on userRole.RoleId equals role.Id
            join account in _dbContext.Set<ApplicationUser>() on userRole.UserId equals account.Id
            where role.Name == roleName && account.TenantId == tenantId
            select userRole.UserId).Distinct().ToListAsync(cancellationToken);

        if (recipientIds.Count == 0)
        {
            _logger.LogInformation("No users hold role '{Role}' in tenant {TenantId}; notification '{Title}' not delivered.",
                roleName, tenantId, title);
            return;
        }

        foreach (var recipientId in recipientIds)
        {
            await NotifyUserAsync(recipientId, kind, title, body, link, cancellationToken);
        }
    }

    private async Task FanOutAsync(Notification notification, User recipient, CancellationToken cancellationToken)
    {
        // Real-time badge update for anyone currently connected.
        try
        {
            await _pusher.PushAsync(recipient.Id, new NotificationPayload(
                notification.Id, notification.Kind, notification.Title,
                notification.Body, notification.Link, notification.CreatedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time push failed for notification {NotificationId}.", notification.Id);
        }

        if (!string.IsNullOrWhiteSpace(recipient.Email) && _emailSender.Enabled)
        {
            try
            {
                await _emailSender.SendAsync(recipient.Email, notification.Title,
                    BuildEmailBody(notification), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email delivery failed for notification {NotificationId}.", notification.Id);
            }
        }

        if (!string.IsNullOrEmpty(recipient.TelegramChatId))
        {
            try
            {
                await _messageGateway.SendMessageAsync(recipient.TelegramChatId,
                    $"{notification.Title}\n{notification.Body}", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram delivery failed for notification {NotificationId}.", notification.Id);
            }
        }
    }

    private static string BuildEmailBody(Notification notification)
    {
        var action = string.IsNullOrWhiteSpace(notification.Link)
            ? string.Empty
            : $"""<p style="margin:24px 0"><a href="{notification.Link}" style="background:#0ea5e9;color:#ffffff;padding:10px 18px;border-radius:8px;text-decoration:none">Open in WerkPilot</a></p>""";

        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;padding:24px">
              <h2 style="color:#0f172a;margin:0 0 4px">{notification.Title}</h2>
              <p style="color:#334155">{notification.Body}</p>
              {action}
              <p style="color:#94a3b8;font-size:12px">You are receiving this because of activity in your WerkPilot workspace.</p>
            </div>
            """;
    }
}
