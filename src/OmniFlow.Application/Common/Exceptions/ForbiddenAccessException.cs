namespace OmniFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when the authenticated user lacks the role/permission required for
/// the requested operation. Maps to HTTP 403.
/// </summary>
public class ForbiddenAccessException : ApiException
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.")
        : base(message, "api.error.forbidden")
    {
    }
}
