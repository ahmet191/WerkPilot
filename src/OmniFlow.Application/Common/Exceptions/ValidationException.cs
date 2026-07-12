using FluentValidation.Results;

namespace OmniFlow.Application.Common.Exceptions;

/// <summary>
/// Thrown when request input fails validation — either by FluentValidation
/// validators in the MediatR pipeline, or by template-driven dynamic field
/// checks inside handlers. Maps to HTTP 400 with a field-keyed error dictionary.
/// </summary>
public class ValidationException : ApiException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures have occurred.", "api.error.validation")
    {
        Errors = errors;
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this(failures
            .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
            .ToDictionary(g => g.Key, g => g.ToArray()))
    {
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(new Dictionary<string, string[]> { [propertyName] = [errorMessage] })
    {
    }
}
