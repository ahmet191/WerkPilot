namespace OmniFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when the operation conflicts with the current state of the resource
/// (e.g. an illegal workflow transition, or missing prerequisite configuration).
/// Maps to HTTP 409.
/// </summary>
public class ConflictException : ApiException
{
    public ConflictException(string message, string messageKey = "api.error.conflict")
        : base(message, messageKey)
    {
    }
}
