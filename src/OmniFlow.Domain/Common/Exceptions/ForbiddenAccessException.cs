namespace OmniFlow.Domain.Common.Exceptions;

/// <summary>Thrown when the acting user lacks the role/permission required for
/// the operation. Maps to HTTP 403.</summary>
public class ForbiddenAccessException : ApiException
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.")
        : base(message, "api.error.forbidden")
    {
    }
}
