namespace OmniFlow.Domain.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist (or is invisible to
/// the current tenant). Maps to HTTP 404.</summary>
public class NotFoundException : ApiException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.", "api.error.notfound")
    {
    }

    public NotFoundException(string message)
        : base(message, "api.error.notfound")
    {
    }
}
