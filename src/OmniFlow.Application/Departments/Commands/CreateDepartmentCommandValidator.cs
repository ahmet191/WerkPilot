using FluentValidation;

namespace OmniFlow.Application.Departments.Commands;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A department name is required.")
            .MaximumLength(200);
    }
}
