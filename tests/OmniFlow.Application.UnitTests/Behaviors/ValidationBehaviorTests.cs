using FluentValidation;
using OmniFlow.Application.Common.Behaviors;
using ValidationException = OmniFlow.Domain.Common.Exceptions.ValidationException;

namespace OmniFlow.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    // A trivial request/response pair for the pipeline.
    public record Ping(string Name) : MediatR.IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    private static ValidationBehavior<Ping, string> Behavior(params IValidator<Ping>[] validators) =>
        new(validators);

    [Fact]
    public async Task Passes_through_when_no_validators()
    {
        var behavior = Behavior();
        var result = await behavior.Handle(new Ping("ok"), _ => Task.FromResult("handled"), CancellationToken.None);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Passes_through_when_validation_succeeds()
    {
        var behavior = Behavior(new PingValidator());
        var result = await behavior.Handle(new Ping("ok"), _ => Task.FromResult("handled"), CancellationToken.None);
        Assert.Equal("handled", result);
    }

    [Fact]
    public async Task Throws_ValidationException_and_short_circuits_when_validation_fails()
    {
        var behavior = Behavior(new PingValidator());
        var handlerRan = false;

        var ex = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new Ping(""), _ => { handlerRan = true; return Task.FromResult("handled"); }, CancellationToken.None));

        Assert.False(handlerRan);
        Assert.True(ex.Errors.ContainsKey(nameof(Ping.Name)));
    }
}
