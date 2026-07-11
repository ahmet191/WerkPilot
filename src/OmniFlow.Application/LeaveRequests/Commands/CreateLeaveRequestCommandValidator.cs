using FluentValidation;

namespace OmniFlow.Application.LeaveRequests.Commands;

public class CreateLeaveRequestCommandValidator : AbstractValidator<CreateLeaveRequestCommand>
{
    private const int MaxLeaveDurationDays = 365;

    public CreateLeaveRequestCommandValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("A valid leave type is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("A start date is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("An end date is required.")
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after the start date.");

        RuleFor(x => x)
            .Must(x => (x.EndDate - x.StartDate).TotalDays <= MaxLeaveDurationDays)
            .WithName(nameof(CreateLeaveRequestCommand.EndDate))
            .WithMessage($"A leave request cannot span more than {MaxLeaveDurationDays} days.");

        RuleFor(x => x.Reason)
            .MaximumLength(1000);
    }
}
