using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UiSchemaController : ControllerBase
{
    private readonly IUserContext _userContext;

    public UiSchemaController(IUserContext userContext)
    {
        _userContext = userContext;
    }

    [HttpGet("{entityType}")]
    public IActionResult GetSchema(string entityType)
    {
        var schema = entityType.ToLower() switch
        {
            "departments" => GetDepartmentsSchema(),
            "assets" => GetAssetsSchema(),
            "tickets" => GetTicketsSchema(),
            "dashboard" => GetDashboardSchema(),
            "leaves" => GetLeavesSchema(),
            "auditlogs" => _userContext.Role == "Admin" ? GetAuditLogsSchema() : null,
            _ => null
        };

        if (schema == null)
            return NotFound();

        return Ok(schema);
    }

    private UiSchema GetDepartmentsSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "departments.title",
                Endpoint = "/departments",
                Actions = new List<string> { "add", "export", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "id", TitleKey = "departments.id", Width = 250, Ellipsis = true },
                    new() { DataIndex = "name", TitleKey = "departments.name", Sortable = true },
                    new() { DataIndex = "managerId", TitleKey = "departments.manager" }
                }
            },
            Form = new FormSchema
            {
                TitleKey = "departments.create",
                SubmitEndpoint = "/departments",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "departments.name", Type = "text", Required = true, MinLength = 3, MaxLength = 100 },
                    new() { Name = "managerId", LabelKey = "departments.manager", Type = "select", Required = false, DataSourceEndpoint = "/lookup/users" },
                    new() { Name = "parentDepartmentId", LabelKey = "departments.id", Type = "text" }
                }
            }
        };
    }

    private UiSchema GetAssetsSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "assets.title",
                Endpoint = "/assets",
                Actions = new List<string> { "add", "export", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "assetTag", TitleKey = "assets.tag", Width = 120 },
                    new() { DataIndex = "category", TitleKey = "assets.category", Width = 150 },
                    new() { DataIndex = "model", TitleKey = "assets.model" },
                    new() { DataIndex = "serialNumber", TitleKey = "assets.serial" },
                    new() { DataIndex = "status", TitleKey = "assets.status", Type = "tag", Width = 100 },
                    new() { DataIndex = "assignedToUserId", TitleKey = "assets.assigned", Ellipsis = true }
                }
            },
            Form = new FormSchema
            {
                TitleKey = "assets.create",
                SubmitEndpoint = "/assets",
                Fields = new List<FormField>
                {
                    new() { Name = "assetTag", LabelKey = "assets.tag", Type = "text", Required = true, RegexPattern = "^[A-Z]{3}-\\d{4}$" },
                    new() { Name = "model", LabelKey = "assets.model", Type = "text", Required = true },
                    new() { Name = "serialNumber", LabelKey = "assets.serial", Type = "text", Required = true },
                    new() { 
                        Name = "category", 
                        LabelKey = "assets.category", 
                        Type = "select", 
                        Required = true,
                        Options = new List<FieldOption>
                        {
                            new() { Label = "Laptop", Value = "Laptop" },
                            new() { Label = "Mobile", Value = "Mobile" },
                            new() { Label = "Monitor", Value = "Monitor" }
                        }
                    },
                    new() { 
                        Name = "status", 
                        LabelKey = "assets.status", 
                        Type = "select", 
                        Required = true,
                        Options = new List<FieldOption>
                        {
                            new() { Label = "In Stock", Value = "InStock" },
                            new() { Label = "Assigned", Value = "Assigned" },
                            new() { Label = "Broken", Value = "Broken" }
                        }
                    }
                }
            }
        };
    }

    private UiSchema GetTicketsSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "sidebar.tickets",
                Endpoint = "/tickets/requester/11111111-1111-1111-1111-111111111111", // Hardcoded mock user
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "id", TitleKey = "tickets.id", Width = 250, Ellipsis = true },
                    new() { DataIndex = "currentState", TitleKey = "tickets.state", Type = "tag", Width = 120 },
                    new() { DataIndex = "slaDueDate", TitleKey = "tickets.deadline", Type = "date" }
                }
            },
            Form = new FormSchema
            {
                TitleKey = "tickets.create",
                SubmitEndpoint = "/tickets",
                TemplateEndpoint = "/api/templates",
                DynamicSchemaEndpoint = "/api/uischema/forms/tickets/{templateId}"
            }
        };
    }

    private UiSchema GetDashboardSchema()
    {
        return new UiSchema
        {
            Widgets = new List<WidgetSchema>
            {
                new() { TitleKey = "dashboard.tickets", Type = "statistic", DataKey = "open_tickets", Endpoint = "/dashboard/metrics", Icon = "ticket", Color = "#1890ff" },
                new() { TitleKey = "dashboard.slabreaches", Type = "statistic", DataKey = "sla_breaches", Endpoint = "/dashboard/metrics", Icon = "exclamation", Color = "#f5222d" },
                new() { TitleKey = "dashboard.leaves", Type = "statistic", DataKey = "pending_leaves", Endpoint = "/dashboard/metrics", Icon = "fire", Color = "#faad14" },
                new() { TitleKey = "dashboard.assets", Type = "statistic", DataKey = "my_assets", Endpoint = "/dashboard/metrics", Icon = "laptop", Color = "#52c41a" }
            }
        };
    }

    private UiSchema GetLeavesSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                Endpoint = "/leaverequests/my",
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "type", TitleKey = "leaves.type", Width = 150, Sortable = true },
                    new() { DataIndex = "startDate", TitleKey = "leaves.startDate", Type = "date", Width = 150, Sortable = true },
                    new() { DataIndex = "endDate", TitleKey = "leaves.endDate", Type = "date", Width = 150, Sortable = true },
                    new() { DataIndex = "status", TitleKey = "leaves.status", Type = "tag", Width = 120, Sortable = true }
                },
                Actions = new List<string> { "add" },
                RowActions = _userContext.Role == "Admin" || _userContext.Role == "Manager" ? new List<GridAction>
                {
                    new() { LabelKey = "leaves.approve", Method = "PUT", Endpoint = "/leaverequests/{id}/approve", Color = "primary" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "leaves.create",
                SubmitEndpoint = "/leaverequests",
                Fields = new List<FormField>
                {
                    new() { Name = "type", LabelKey = "leaves.type", Type = "select", Required = true, DataSourceEndpoint = "/lookup/leavetypes" },
                    new() { Name = "startDate", LabelKey = "leaves.startDate", Type = "date", Required = true },
                    new() { Name = "endDate", LabelKey = "leaves.endDate", Type = "date", Required = true },
                    new() { Name = "reason", LabelKey = "leaves.reason", Type = "text" }
                }
            }
        };
    }

    private UiSchema GetAuditLogsSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                Endpoint = "/auditlogs",
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "timestamp", TitleKey = "auditlogs.timestamp", Type = "date", Width = 180, Sortable = true },
                    new() { DataIndex = "action", TitleKey = "auditlogs.action", Type = "tag", Width = 100, Sortable = true },
                    new() { DataIndex = "entityType", TitleKey = "auditlogs.entityType", Width = 150, Sortable = true },
                    new() { DataIndex = "details", TitleKey = "auditlogs.details", Width = 300 }
                }
            }
        };
    }

    [HttpGet("forms/{entityName}")]
    public async Task<IActionResult> GetFormSchema(string entityName)
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
            Fields = new List<FormField>
            {
                new() { Name = "templateId", LabelKey = "tickets.templateId", Type = "text", Required = true }
                // Here we map dynamic fields from the JSON
            }
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
