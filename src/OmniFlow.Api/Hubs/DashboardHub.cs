using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace OmniFlow.Api.Hubs;

[Authorize]
public class DashboardHub : Hub
{
    // Clients will connect here. We can add grouping by TenantId later if needed.
    // For now, simple broadcast to all connected dashboard users.

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
