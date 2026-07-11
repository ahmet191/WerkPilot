using FluentValidation;

namespace OmniFlow.Application.Users.Commands;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("An email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(320);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("A valid role is required.");

        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("A department is required.");
    }
}
