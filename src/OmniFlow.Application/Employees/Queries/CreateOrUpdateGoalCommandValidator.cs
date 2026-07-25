using FluentValidation;

namespace OmniFlow.Application.Employees.Queries;

public class CreateOrUpdateGoalCommandValidator : AbstractValidator<CreateOrUpdateGoalCommand>
{
    public CreateOrUpdateGoalCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("A goal title is required.")
            .MaximumLength(200);

        RuleFor(x => x.Quarter)
            .Must(q => q is "Q1" or "Q2" or "Q3" or "Q4" or "Annual")
            .WithMessage("Quarter must be Q1–Q4 or Annual.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.TargetValue)
            .GreaterThan(0).WithMessage("Target value must be greater than zero.");

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0).WithMessage("Current value cannot be negative.");

        RuleFor(x => x.Unit)
            .NotEmpty().MaximumLength(50);

        RuleFor(x => x.Status)
            .Must(s => s is "NotStarted" or "InProgress" or "Behind" or "Achieved")
            .WithMessage("Status must be NotStarted, InProgress, Behind or Achieved.");
    }
}
