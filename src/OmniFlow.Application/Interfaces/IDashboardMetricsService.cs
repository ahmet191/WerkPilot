namespace OmniFlow.Application.Interfaces;

public interface IDashboardMetricsService
{
    Task BroadcastMetricUpdateAsync(string widgetId, int incrementValue, CancellationToken cancellationToken = default);
}
