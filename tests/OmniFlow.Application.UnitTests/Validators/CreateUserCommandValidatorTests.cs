using FluentValidation.TestHelper;
using OmniFlow.Application.Users.Commands;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.UnitTests.Validators;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand Valid() =>
        new("Alice Example", "alice@example.com", UserRole.Requester, Guid.NewGuid());

    [Fact]
    public void Valid_command_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Invalid_email_fails(string email)
    {
        var cmd = Valid() with { Email = email };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Empty_name_fails()
    {
        var cmd = Valid() with { Name = "" };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Empty_department_fails()
    {
        var cmd = Valid() with { DepartmentId = Guid.Empty };
        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.DepartmentId);
    }
}
