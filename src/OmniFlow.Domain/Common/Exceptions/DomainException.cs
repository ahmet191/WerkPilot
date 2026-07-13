namespace OmniFlow.Domain.Common.Exceptions;

/// <summary>
/// Base for all domain/application error conditions that map to HTTP responses.
/// Lives in the Domain (innermost) layer so aggregates can enforce invariants
/// with the right error contract; the API's exception middleware maps derived
/// types to status codes. <see cref="MessageKey"/> is a stable, translatable key
/// consumed by the frontend localization layer.
/// </summary>
public class DomainException : Exception
{
    public string MessageKey { get; }

    public DomainException(string messageKey) : base(messageKey)
    {
        MessageKey = messageKey;
    }

    public DomainException(string message, string messageKey) : base(message)
    {
        MessageKey = messageKey;
    }
}

// Backwards-compatible alias: the codebase (and tests) referred to the base as
// ApiException. Keeping the name avoids a wide rename while the type now lives
// in the Domain layer.
public class ApiException : DomainException
{
    public ApiException(string messageKey) : base(messageKey) { }
    public ApiException(string message, string messageKey) : base(message, messageKey) { }
}
