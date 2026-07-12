using FluentValidation;

namespace OmniFlow.Application.Tickets.Commands;

/// <summary>
/// Shape validation for ticket creation. Template-driven dynamic field rules
/// (required/dropdown/regex from the template's JSONB field definitions) are
/// enforced in the handler, which already loads the template row.
/// </summary>
public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("A ticket template must be selected.");

        RuleFor(x => x.RequesterId)
            .NotEmpty().WithMessage("A requester is required.");

        RuleFor(x => x.DynamicData)
            .NotNull().WithMessage("Ticket data is required.");
    }
}
