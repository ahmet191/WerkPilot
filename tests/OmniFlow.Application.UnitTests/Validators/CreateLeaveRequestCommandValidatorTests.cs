using FluentValidation.TestHelper;
using OmniFlow.Application.LeaveRequests.Commands;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.UnitTests.Validators;

public class CreateLeaveRequestCommandValidatorTests
{
    private readonly CreateLeaveRequestCommandValidator _validator = new();

    private static CreateLeaveRequestCommand Valid() => new()
    {
        Type = LeaveType.Annual,
        StartDate = new DateTime(2026, 8, 1),
        EndDate = new DateTime(2026, 8, 5),
        Reason = "Vacation",
    };

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EndDate_before_StartDate_fails()
    {
        var cmd = Valid();
        cmd.EndDate = cmd.StartDate.AddDays(-1);

        _validator.TestValidate(cmd)
            .ShouldHaveValidationErrorFor(x => x.EndDate)
            .WithErrorMessage("End date must be after the start date.");
    }

    [Fact]
    public void EndDate_equal_to_StartDate_fails()
    {
        var cmd = Valid();
        cmd.EndDate = cmd.StartDate;

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.EndDate);
    }

    [Fact]
    public void Leave_longer_than_a_year_fails()
    {
        var cmd = Valid();
        cmd.EndDate = cmd.StartDate.AddDays(400);

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(nameof(CreateLeaveRequestCommand.EndDate));
    }

    [Fact]
    public void Reason_over_1000_chars_fails()
    {
        var cmd = Valid();
        cmd.Reason = new string('x', 1001);

        _validator.TestValidate(cmd).ShouldHaveValidationErrorFor(x => x.Reason);
    }
}
