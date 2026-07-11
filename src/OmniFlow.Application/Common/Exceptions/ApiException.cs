namespace OmniFlow.Application.Common.Exceptions;

/// <summary>
/// Base class for all application-level exceptions that translate to HTTP error
/// responses. Carries a stable, translatable <see cref="MessageKey"/> consumed by
/// the frontend localization layer, alongside a human-readable developer message.
/// Derived types map to specific HTTP status codes in the exception middleware;
/// a plain <see cref="ApiException"/> maps to 400 Bad Request.
/// </summary>
public class ApiException : Exception
{
    public string MessageKey { get; }

    public ApiException(string messageKey) : base(messageKey)
    {
        MessageKey = messageKey;
    }

    public ApiException(string message, string messageKey) : base(message)
    {
        MessageKey = messageKey;
    }
}
