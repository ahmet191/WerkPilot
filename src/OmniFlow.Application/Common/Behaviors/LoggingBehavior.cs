using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior emitting one structured log line per request with
/// the request name, acting user/tenant, and elapsed time. Slow requests are
/// escalated to a warning so they stand out in dashboards.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 1000;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IUserContext _userContext;
    private readonly ITenantService _tenantService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IUserContext userContext,
        ITenantService tenantService)
    {
        _logger = logger;
        _userContext = userContext;
        _tenantService = tenantService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "Slow request {RequestName} completed in {ElapsedMs}ms (User {UserId}, Tenant {TenantId})",
                    requestName, stopwatch.ElapsedMilliseconds, _userContext.UserId, _tenantService.TenantId);
            }
            else
            {
                _logger.LogInformation(
                    "{RequestName} completed in {ElapsedMs}ms (User {UserId}, Tenant {TenantId})",
                    requestName, stopwatch.ElapsedMilliseconds, _userContext.UserId, _tenantService.TenantId);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            // Expected business exceptions are logged as warnings by the HTTP
            // middleware; here we only annotate which request failed.
            _logger.LogDebug(ex,
                "{RequestName} failed after {ElapsedMs}ms (User {UserId}, Tenant {TenantId})",
                requestName, stopwatch.ElapsedMilliseconds, _userContext.UserId, _tenantService.TenantId);
            throw;
        }
    }
}
