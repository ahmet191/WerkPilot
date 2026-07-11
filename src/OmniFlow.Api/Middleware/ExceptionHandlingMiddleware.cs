using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Common.Exceptions;

namespace OmniFlow.Api.Middleware;

/// <summary>
/// Global exception handler translating application exceptions into RFC 7807
/// ProblemDetails responses. Every response carries a <c>traceId</c> extension
/// (correlatable with structured logs / OpenTelemetry traces) and a stable
/// <c>messageKey</c> extension consumed by the frontend localization layer.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problem = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Detail = validationEx.Message,
                Extensions = { ["errors"] = validationEx.Errors }
            },
            NotFoundException notFoundEx => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "The specified resource was not found.",
                Detail = notFoundEx.Message
            },
            ForbiddenAccessException forbiddenEx => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                Title = "Forbidden.",
                Detail = forbiddenEx.Message
            },
            ConflictException conflictEx => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
                Title = "The request conflicts with the current state of the resource.",
                Detail = conflictEx.Message
            },
            ApiException apiEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "Bad request.",
                Detail = apiEx.Message
            },
            KeyNotFoundException => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                Title = "The specified resource was not found."
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                Title = "An unexpected error occurred.",
                // Never leak internals: the traceId is enough to find the full
                // exception in the structured logs.
                Detail = "An internal server error has occurred. Contact support with the traceId."
            }
        };

        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
        problem.Extensions["messageKey"] = exception switch
        {
            ApiException apiEx => apiEx.MessageKey,
            KeyNotFoundException => "api.error.notfound",
            _ => "api.error.generic"
        };

        // Expected business failures are warnings; only genuine faults are errors.
        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception,
                "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Request {Method} {Path} failed with {StatusCode}: {Reason}",
                context.Request.Method, context.Request.Path, problem.Status, exception.Message);
        }

        context.Response.StatusCode = problem.Status!.Value;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
