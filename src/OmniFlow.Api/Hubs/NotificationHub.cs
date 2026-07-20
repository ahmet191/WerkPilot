using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OmniFlow.Application.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Hubs;

/// <summary>
/// Live delivery of notifications. Each connection joins a group named after
/// the signed-in user, so a push reaches every tab that user has open and
/// nobody else's.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    public static string GroupFor(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(Claims.Subject)
                     ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (Guid.TryParse(userId, out var id))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(id));
        }

        await base.OnConnectedAsync();
    }
}

/// <summary>SignalR-backed <see cref="INotificationPusher"/>.</summary>
public class SignalRNotificationPusher : INotificationPusher
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationPusher(IHubContext<NotificationHub> hub)
    {
        _hub = hub;
    }

    public Task PushAsync(Guid recipientUserId, NotificationPayload payload, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(NotificationHub.GroupFor(recipientUserId))
            .SendAsync("notification", payload, cancellationToken);
}
