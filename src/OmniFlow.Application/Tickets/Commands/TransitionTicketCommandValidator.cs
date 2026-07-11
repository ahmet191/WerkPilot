using FluentValidation;

namespace OmniFlow.Application.Tickets.Commands;

public class TransitionTicketCommandValidator : AbstractValidator<TransitionTicketCommand>
{
    public TransitionTicketCommandValidator()
    {
        RuleFor(x => x.TicketId)
            .NotEmpty().WithMessage("A ticket id is required.");

        RuleFor(x => x.Trigger)
            .NotEmpty().WithMessage("A workflow trigger is required.")
            .MaximumLength(100);

        RuleFor(x => x.ChangedById)
            .NotEmpty().WithMessage("The acting user is required.");
    }
}
