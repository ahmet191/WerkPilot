using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Middleware;

/// <summary>
/// Writes an audit trail row for every mutating API request. Runs after
/// authentication so the entry carries the real user. Only /api/* paths are
/// audited — credentials posted to /connect/token must never be captured.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOmniFlowDbContext dbContext, IUserContext userContext)
    {
        var method = context.Request.Method;
        var isMutation = method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Delete;
        var isApiPath = context.Request.Path.StartsWithSegments("/api");

        if (isMutation && isApiPath && context.User.Identity?.IsAuthenticated == true)
        {
            string? body = null;
            var contentType = context.Request.ContentType ?? string.Empty;

            if (contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                body = "[Multipart File Data - Omitted]";
            }
            else
            {
                context.Request.EnableBuffering();
                body = await new StreamReader(context.Request.Body).ReadToEndAsync();
                context.Request.Body.Position = 0;
            }

            var entityType = context.Request.Path.Value?.Split('/').Skip(2).FirstOrDefault() ?? "Unknown";

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userContext.UserId,
                Action = method,
                EntityType = entityType,
                Details = string.IsNullOrEmpty(body) ? null : body,
                Timestamp = DateTime.UtcNow
            };

            dbContext.AuditLogs.Add(auditLog);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        await _next(context);
    }
}
