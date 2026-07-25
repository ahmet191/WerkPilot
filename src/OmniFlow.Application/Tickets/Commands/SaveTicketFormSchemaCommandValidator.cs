using FluentValidation;

namespace OmniFlow.Application.Tickets.Commands;

public class SaveTicketFormSchemaCommandValidator : AbstractValidator<SaveTicketFormSchemaCommand>
{
    public SaveTicketFormSchemaCommandValidator()
    {
        RuleFor(x => x.ServiceDeskId)
            .NotEqual(Guid.Empty).WithMessage("A service desk must be selected.");

        RuleFor(x => x.FormTitle)
            .NotEmpty().WithMessage("A form title is required.")
            .MaximumLength(200);

        RuleFor(x => x.FormDescription)
            .MaximumLength(1000);

        // The designer serialises its fields to JSON; guard against an empty or
        // malformed payload before the handler tries to persist it.
        RuleFor(x => x.FieldsJson)
            .NotEmpty().WithMessage("A form definition is required.")
            .Must(BeValidJsonArray).WithMessage("The form definition must be a valid JSON array.");
    }

    private static bool BeValidJsonArray(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
