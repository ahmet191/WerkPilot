using System.Text;
using Microsoft.AspNetCore.Http;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOmniFlowDbContext dbContext, IUserContext userContext)
    {
        // Intercept POST, PUT, DELETE requests
        var method = context.Request.Method;
        if (method == HttpMethods.Post || method == HttpMethods.Put || method == HttpMethods.Delete)
        {
            context.Request.EnableBuffering();
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            context.Request.Body.Position = 0;

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
