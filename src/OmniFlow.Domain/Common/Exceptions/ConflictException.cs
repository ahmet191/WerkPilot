namespace OmniFlow.Domain.Common.Exceptions;

/// <summary>Thrown when an operation conflicts with the current state of the
/// resource (illegal workflow transition, missing prerequisite). Maps to HTTP 409.</summary>
public class ConflictException : ApiException
{
    public ConflictException(string message, string messageKey = "api.error.conflict")
        : base(message, messageKey)
    {
    }
}
