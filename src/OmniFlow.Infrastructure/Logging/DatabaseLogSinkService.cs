using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Persistence;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace OmniFlow.Infrastructure.Logging;

public class DatabaseLogSinkService : BackgroundService
{
    private readonly ChannelReader<LogEvent> _channelReader;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseLogSinkService> _logger;

    public DatabaseLogSinkService(
        ChannelReader<LogEvent> channelReader,
        IServiceProvider serviceProvider,
        ILogger<DatabaseLogSinkService> logger)
    {
        _channelReader = channelReader;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var batch = new List<LogEvent>(100);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Wait for at least one log event
                if (await _channelReader.WaitToReadAsync(stoppingToken))
                {
                    while (_channelReader.TryRead(out var logEvent) && batch.Count < 100)
                    {
                        batch.Add(logEvent);
                    }

                    if (batch.Count > 0)
                    {
                        await WriteBatchAsync(batch, stoppingToken);
                        batch.Clear();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                // We cannot use Serilog here easily without risking recursion,
                // but _logger is safe as long as this specific class's errors 
                // aren't fed back into the channel infinitely.
                Console.WriteLine($"[DatabaseLogSinkService] Failed to write batch: {ex}");
                batch.Clear();
            }
        }
    }

    private async Task WriteBatchAsync(List<LogEvent> batch, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IOmniFlowDbContext>();

        var systemLogs = new List<SystemLog>(batch.Count);

        foreach (var logEvent in batch)
        {
            // Extract TraceId, TenantId, UserId if present
            string? traceId = null;
            Guid? tenantId = null;
            Guid? userId = null;

            if (logEvent.Properties.TryGetValue("TraceId", out var traceIdValue) && traceIdValue is ScalarValue tv && tv.Value is string t)
                traceId = t;
            else if (logEvent.Properties.TryGetValue("CorrelationId", out var corrIdValue) && corrIdValue is ScalarValue cv && cv.Value is string c)
                traceId = c;

            if (logEvent.Properties.TryGetValue("TenantId", out var tenantIdValue) && tenantIdValue is ScalarValue tenv && tenv.Value is Guid tg)
                tenantId = tg;

            if (logEvent.Properties.TryGetValue("UserId", out var userIdValue) && userIdValue is ScalarValue uv && uv.Value is Guid ug)
                userId = ug;

            // Serialize all properties to JSONB
            var propertiesDict = logEvent.Properties.ToDictionary(p => p.Key, p => p.Value.ToString());
            var propertiesJson = JsonSerializer.Serialize(propertiesDict);

            systemLogs.Add(new SystemLog
            {
                Id = Guid.NewGuid(),
                Timestamp = logEvent.Timestamp,
                Level = logEvent.Level.ToString(),
                MessageTemplate = logEvent.MessageTemplate.Text,
                Message = logEvent.RenderMessage(),
                Exception = logEvent.Exception?.ToString(),
                Properties = propertiesJson,
                TraceId = traceId,
                TenantId = tenantId,
                UserId = userId
            });
        }

        dbContext.SystemLogs.AddRange(systemLogs);
        
        // We use CancellationToken.None here to ensure the batch saves even if the app is shutting down.
        await dbContext.SaveChangesAsync(CancellationToken.None);
    }
}
