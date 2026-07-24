using Microsoft.AspNetCore.SignalR;
using OmniFlow.Api.Hubs;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Services;

public class DashboardMetricsService : IDashboardMetricsService
{
    private readonly IHubContext<DashboardHub> _hubContext;

    public DashboardMetricsService(IHubContext<DashboardHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task BroadcastMetricUpdateAsync(string widgetId, int incrementValue, CancellationToken cancellationToken = default)
    {
        // Broadcasts to all connected clients a metric update
        await _hubContext.Clients.All.SendAsync("ReceiveMetricUpdate", new { WidgetId = widgetId, IncrementValue = incrementValue }, cancellationToken);
    }
}
