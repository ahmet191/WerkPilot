namespace OmniFlow.Domain.Common.Exceptions;

/// <summary>
/// Thrown when input fails validation — by the MediatR FluentValidation pipeline
/// or by template-driven invariants inside aggregates. Maps to HTTP 400 with a
/// field-keyed error dictionary. Deliberately free of any FluentValidation
/// dependency so the Domain layer stays framework-agnostic; the pipeline behavior
/// adapts FluentValidation failures into the dictionary.
/// </summary>
public class ValidationException : ApiException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", "api.error.validation")
    {
        Errors = errors;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]> { [propertyName] = [errorMessage] })
    {
    }
}
