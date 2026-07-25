using FluentValidation;

namespace OmniFlow.Application.Tickets.Commands;

public class SaveSlaPolicyRuleCommandValidator : AbstractValidator<SaveSlaPolicyRuleCommand>
{
    public SaveSlaPolicyRuleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A policy name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Priority)
            .NotEmpty()
            .Must(p => p is "Critical" or "High" or "Medium" or "Low")
            .WithMessage("Priority must be Critical, High, Medium or Low.");

        RuleFor(x => x.ResponseTimeMinutes)
            .GreaterThan(0).WithMessage("Response SLA must be greater than zero.");

        RuleFor(x => x.ResolutionTimeHours)
            .GreaterThan(0).WithMessage("Resolution SLA must be greater than zero.");

        RuleFor(x => x.EscalateAfterPercent)
            .InclusiveBetween(1, 100).WithMessage("Escalation threshold must be between 1 and 100%.");
    }
}
