using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Common.Exceptions;

namespace OmniFlow.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
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
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiErrorResponse();

        switch (exception)
        {
            case ApiException apiEx:
                // Custom application exception
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.MessageKey = apiEx.MessageKey;
                break;
            case KeyNotFoundException:
                // Not found exception
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response.MessageKey = "api.error.notfound";
                break;
            default:
                // Unhandled generic exception
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.MessageKey = "api.error.generic";
                break;
        }

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var result = JsonSerializer.Serialize(response, jsonOptions);

        return context.Response.WriteAsync(result);
    }
}
