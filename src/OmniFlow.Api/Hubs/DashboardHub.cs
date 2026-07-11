using Microsoft.AspNetCore.SignalR;

namespace OmniFlow.Api.Hubs;

public class DashboardHub : Hub
{
    // Clients will connect here. We can add grouping by TenantId later if needed.
    // For now, simple broadcast to all connected dashboard users.
    
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}
