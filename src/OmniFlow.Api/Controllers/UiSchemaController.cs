using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Api.UiSchemas;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Serves the schemas that drive the dynamic grids and forms: the shipped
/// built-in schema merged with whatever the caller's tenant has customized.
/// Presentation (columns, titles) may be overridden per tenant; anything that
/// drives a network call or gates an operation (endpoints, row actions, forms)
/// is always recomputed from the built-in for the requesting user, so an
/// override can never repoint an endpoint or unlock an action. Editing requires
/// <c>uischema.manage</c>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UiSchemaController : ControllerBase
{
    // The stored override and the wire format both use the Web defaults
    // (camelCase, case-insensitive) so a schema round-trips through the editor
    // unchanged.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IUserContext _userContext;
    private readonly IUiSchemaService _uiSchemas;

    public UiSchemaController(IUserContext userContext, IUiSchemaService uiSchemas)
    {
        _userContext = userContext;
        _uiSchemas = uiSchemas;
    }

    // Identity roles are "Administrator"/"HRAdmin"/...; legacy checks used "Admin".
    private bool IsAdmin => _userContext.Role is "Admin" or "Administrator";

    private UiSchemaContext SchemaContext => new(IsAdmin, _userContext.Role);

    // The editor customizes against the fullest schema regardless of the admin's
    // own role, so they can shape screens they might not personally see.
    private static UiSchemaContext EditorContext => new(true, Permissions.SuperRole);

    [HttpGet("{entityType}")]
    public async Task<IActionResult> GetSchema(string entityType)
    {
        var builtIn = BuiltInUiSchemas.For(entityType, SchemaContext);
        if (builtIn is null) return NotFound();

        var overrideJson = await _uiSchemas.GetOverrideAsync(entityType, HttpContext.RequestAborted);
        if (string.IsNullOrEmpty(overrideJson)) return Ok(builtIn);

        UiSchema? overridden;
        try
        {
            overridden = JsonSerializer.Deserialize<UiSchema>(overrideJson, JsonOptions);
        }
        catch (JsonException)
        {
            // A corrupt override must never break the screen — fall back to shipped.
            return Ok(builtIn);
        }

        return overridden is null ? Ok(builtIn) : Ok(MergeSecure(builtIn, overridden));
    }

    /// <summary>
    /// Overlays the tenant's presentation choices onto the shipped schema while
    /// keeping every security-sensitive part from the built-in. The tenant owns
    /// the grid's title and columns; the grid endpoint, its actions and row
    /// actions, and the whole form and widget set are always the shipped values
    /// recomputed for this request's role.
    /// </summary>
    private static UiSchema MergeSecure(UiSchema builtIn, UiSchema overridden) => new()
    {
        Grid = new GridSchema
        {
            TitleKey = overridden.Grid.TitleKey,
            Columns = overridden.Grid.Columns,
            Endpoint = builtIn.Grid.Endpoint,
            Actions = builtIn.Grid.Actions,
            RowActions = builtIn.Grid.RowActions,
        },
        Form = builtIn.Form,
        Widgets = builtIn.Widgets,
    };

    // ----- Admin (uischema.manage) ---------------------------------------

    /// <summary>Every customizable entity type, flagged if the tenant already overrides it.</summary>
    [HttpGet("admin/customizable")]
    [Authorize(Policy = PermissionPolicyNames.UiSchemaManage)]
    public async Task<IActionResult> GetCustomizable()
    {
        var customized = (await _uiSchemas.GetCustomizedEntityTypesAsync(HttpContext.RequestAborted))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = BuiltInUiSchemas.EntityTypes
            .Select(e => new { entityType = e, isCustomized = customized.Contains(e) })
            .ToList();

        return Ok(items);
    }

    /// <summary>The shipped default and the tenant's raw override (if any) for one entity, for the editor.</summary>
    [HttpGet("admin/{entityType}")]
    [Authorize(Policy = PermissionPolicyNames.UiSchemaManage)]
    public async Task<IActionResult> GetForEditing(string entityType)
    {
        var builtIn = BuiltInUiSchemas.For(entityType, EditorContext);
        if (builtIn is null) return NotFound();

        var overrideJson = await _uiSchemas.GetOverrideAsync(entityType, HttpContext.RequestAborted);
        var overridden = string.IsNullOrEmpty(overrideJson)
            ? null
            : JsonSerializer.Deserialize<UiSchema>(overrideJson, JsonOptions);

        return Ok(new
        {
            entityType = entityType.ToLowerInvariant(),
            @default = builtIn,
            @override = overridden,
            isCustomized = overridden is not null,
        });
    }

    public record SetUiSchemaRequest([Required] UiSchema Schema);

    [HttpPut("admin/{entityType}")]
    [Authorize(Policy = PermissionPolicyNames.UiSchemaManage)]
    public async Task<IActionResult> SetOverride(string entityType, [FromBody] SetUiSchemaRequest request)
    {
        // Only a known entity type can be customized — never store schemas for
        // something the platform doesn't ship.
        if (BuiltInUiSchemas.For(entityType, EditorContext) is null) return NotFound();

        var json = JsonSerializer.Serialize(request.Schema, JsonOptions);
        await _uiSchemas.SetAsync(entityType, json, HttpContext.RequestAborted);
        return NoContent();
    }

    /// <summary>Drops the customization, reverting the entity to the shipped schema.</summary>
    [HttpDelete("admin/{entityType}")]
    [Authorize(Policy = PermissionPolicyNames.UiSchemaManage)]
    public async Task<IActionResult> RemoveOverride(string entityType)
    {
        await _uiSchemas.RemoveAsync(entityType, HttpContext.RequestAborted);
        return NoContent();
    }

    // ----- Form schemas (template-driven, not overridable) ----------------

    [HttpGet("forms/{entityName}")]
    public IActionResult GetFormSchema(string entityName)
    {
        switch (entityName.ToLower())
        {
            case "leaverequests":
                return Ok(new FormSchema
                {
                    TitleKey = "forms.leaveRequest.title",
                    SubmitEndpoint = "/leaverequests",
                    Fields = new List<FormField>
                    {
                        new() { Name = "type", LabelKey = "leave.type", Type = "select", Required = true, Options = new List<FieldOption> {
                            new() { Label = "Annual", Value = "0" },
                            new() { Label = "Sick", Value = "1" },
                            new() { Label = "Unpaid", Value = "2" }
                        }},
                        new() { Name = "startDate", LabelKey = "leave.startDate", Type = "date", Required = true },
                        new() { Name = "endDate", LabelKey = "leave.endDate", Type = "date", Required = true },
                        new() { Name = "reason", LabelKey = "leave.reason", Type = "text", Required = false }
                    }
                });
            case "departments":
                return Ok(new FormSchema
                {
                    TitleKey = "forms.department.title",
                    SubmitEndpoint = "/departments",
                    Fields = new List<FormField>
                    {
                        new() { Name = "name", LabelKey = "department.name", Type = "text", Required = true }
                    }
                });
            default:
                return NotFound($"Form schema for '{entityName}' not found.");
        }
    }

    [HttpGet("forms/tickets/{templateId}")]
    public async Task<IActionResult> GetTicketFormSchema(Guid templateId, [FromServices] IOmniFlowDbContext dbContext)
    {
        var template = await dbContext.TicketTemplates.FindAsync(templateId);
        if (template == null) return NotFound();

        var userLang = _userContext.Language ?? "en";
        var formSchema = new FormSchema
        {
            TitleKey = template.LocalizedName.GetValueOrDefault(userLang),
            SubmitEndpoint = "/tickets",
            Method = "POST",
            Fields = new List<FormField>()
        };

        if (template.Fields != null)
        {
            foreach (var field in template.Fields)
            {
                var formField = new FormField
                {
                    Name = field.Key,
                    LabelKey = field.LocalizedName.GetValueOrDefault(userLang), // We use the direct translated string as label
                    Type = field.Type.ToLower() == "dropdown" ? "select" : "text",
                    Required = field.IsRequired,
                    RegexPattern = field.ValidationRegex ?? string.Empty
                };

                if (field.DropdownOptions != null && field.DropdownOptions.Any())
                {
                    formField.Options = field.DropdownOptions.Select(o => new FieldOption { Label = o, Value = o }).ToList();
                }

                formSchema.Fields.Add(formField);
            }
        }

        return Ok(formSchema);
    }
}
