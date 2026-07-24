using FluentValidation;
using MediatR;
using OmniFlow.Domain.Common.Exceptions;

namespace OmniFlow.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior running every registered FluentValidation validator
/// for the incoming request. Failures short-circuit the handler and surface as an
/// HTTP 400 ProblemDetails response (via the application ValidationException).
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f is not null)
                .ToList();

            if (failures.Count != 0)
            {
                // Adapt FluentValidation failures into the framework-agnostic
                // Domain ValidationException (field -> messages).
                var errors = failures
                    .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                    .ToDictionary(g => g.Key, g => g.ToArray());
                throw new OmniFlow.Domain.Common.Exceptions.ValidationException(errors);
            }
        }

        return await next(cancellationToken);
    }
}
