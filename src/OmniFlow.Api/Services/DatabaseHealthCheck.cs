using Microsoft.Extensions.Diagnostics.HealthChecks;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Api.Services;

/// <summary>
/// Readiness check verifying the PostgreSQL database is reachable.
/// Implemented against the DbContext directly to avoid an extra health-check
/// package dependency.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly OmniFlowDbContext _context;

    public DatabaseHealthCheck(OmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", ex);
        }
    }
}
