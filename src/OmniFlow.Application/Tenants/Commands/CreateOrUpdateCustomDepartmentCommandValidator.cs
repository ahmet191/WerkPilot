using FluentValidation;

namespace OmniFlow.Application.Tenants.Commands;

public class CreateOrUpdateCustomDepartmentCommandValidator : AbstractValidator<CreateOrUpdateCustomDepartmentCommand>
{
    public CreateOrUpdateCustomDepartmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("A department name is required.")
            .MaximumLength(200);

        // Code is optional in the studio form; only bound when supplied.
        RuleFor(x => x.Code)
            .MaximumLength(32);

        RuleFor(x => x.DepartmentType)
            .NotEmpty().MaximumLength(50);

        // A colour is optional, but when given it must be a valid hex swatch so
        // the theme token it feeds never renders as garbage.
        RuleFor(x => x.ColorHex)
            .Matches("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex))
            .WithMessage("Colour must be a hex value such as #8b5cf6.");
    }
}
