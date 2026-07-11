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
            "servicedesks" => GetServiceDesksSchema(),
            "assets" => GetAssetsSchema(),
            "tickets" => GetTicketsSchema(),
            "dashboard" => GetDashboardSchema(),
            "leaves" => GetLeavesSchema(),
            "expenses" => GetExpensesSchema(),
            "menus" => GetMenusSchema(),
            "users" => GetUsersSchema(),
            "templates" => GetTemplatesSchema(),
            "categories" => GetCategoriesSchema(),
            "slas" => GetSlasSchema(),
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
                    new() { DataIndex = "name", TitleKey = "departments.name", Sortable = true },
                    new() { DataIndex = "managerName", TitleKey = "departments.manager" },
                    new() { DataIndex = "parentDepartmentName", TitleKey = "departments.parentDepartment" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/departments/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/departments/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "departments.create",
                SubmitEndpoint = "/departments",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "departments.name", Type = "text", Required = true, MinLength = 3, MaxLength = 100 },
                    new() { Name = "managerId", LabelKey = "departments.manager", Type = "select", Required = false, DataSourceEndpoint = "/lookup/users" },
                    new() { Name = "parentDepartmentId", LabelKey = "departments.parentDepartment", Type = "select", Required = false, DataSourceEndpoint = "/lookup/departments" }
                }
            }
        };
    }

    private UiSchema GetServiceDesksSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "servicedesks.title",
                Endpoint = "/servicedesks",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "name", TitleKey = "servicedesks.name", Sortable = true },
                    new() { DataIndex = "description", TitleKey = "servicedesks.description" },
                    new() { DataIndex = "isPublicToAllEmployees", TitleKey = "servicedesks.isPublic", Type = "tag" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/servicedesks/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/servicedesks/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "servicedesks.create",
                SubmitEndpoint = "/servicedesks",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "servicedesks.name", Type = "text", Required = true },
                    new() { Name = "description", LabelKey = "servicedesks.description", Type = "text" },
                    new() { Name = "departmentId", LabelKey = "servicedesks.department", Type = "select", DataSourceEndpoint = "/lookup/departments" }
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
                    new() { DataIndex = "assignedToUserName", TitleKey = "assets.assigned" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/assets/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/assets/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
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
                    new() { DataIndex = "title", TitleKey = "tickets.title", Width = 250, Ellipsis = true },
                    new() { DataIndex = "type", TitleKey = "tickets.type", Type = "string", Width = 150 },
                    new() { DataIndex = "priority", TitleKey = "tickets.priority", Type = "tag", Width = 120 },
                    new() { DataIndex = "impact", TitleKey = "tickets.impact", Type = "tag", Width = 120 },
                    new() { DataIndex = "currentState", TitleKey = "tickets.state", Type = "tag", Width = 120 },
                    new() { DataIndex = "slaDueDate", TitleKey = "tickets.deadline", Type = "date" }
                }
            },
            Form = new FormSchema
            {
                TitleKey = "tickets.create",
                SubmitEndpoint = "/tickets",
                TemplateEndpoint = "/templates",
                DynamicSchemaEndpoint = "/uischema/forms/tickets/{templateId}"
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

    private UiSchema GetExpensesSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "expenses.title",
                Endpoint = "/expenseclaims",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "title", TitleKey = "expenses.titleKey", Sortable = true },
                    new() { DataIndex = "amount", TitleKey = "expenses.amount", Sortable = true },
                    new() { DataIndex = "currency", TitleKey = "expenses.currency" },
                    new() { DataIndex = "category", TitleKey = "expenses.category", Type = "tag", Sortable = true },
                    new() { DataIndex = "expenseDate", TitleKey = "expenses.date", Type = "date", Sortable = true },
                    new() { DataIndex = "status", TitleKey = "expenses.status", Type = "tag", Sortable = true }
                }
            },
            Form = new FormSchema
            {
                TitleKey = "expenses.create",
                SubmitEndpoint = "/expenseclaims",
                Fields = new List<FormField>
                {
                    new() { Name = "title", LabelKey = "expenses.titleKey", Type = "text", Required = true },
                    new() { Name = "amount", LabelKey = "expenses.amount", Type = "number", Required = true },
                    new() { 
                        Name = "category", 
                        LabelKey = "expenses.category", 
                        Type = "select", 
                        Required = true,
                        Options = new List<FieldOption>
                        {
                            new() { Label = "Travel", Value = "1" },
                            new() { Label = "Meals", Value = "2" },
                            new() { Label = "Accommodation", Value = "3" },
                            new() { Label = "Equipment", Value = "4" }
                        }
                    },
                    new() { Name = "expenseDate", LabelKey = "expenses.date", Type = "date", Required = true },
                    new() { Name = "description", LabelKey = "expenses.description", Type = "text" }
                }
            }
        };
    }

    private UiSchema GetMenusSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "menus.title",
                Endpoint = "/menu",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "titleKey", TitleKey = "menus.titleKey", Sortable = true },
                    new() { DataIndex = "path", TitleKey = "menus.path" },
                    new() { DataIndex = "icon", TitleKey = "menus.icon" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/menu/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/menu/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "menus.create",
                SubmitEndpoint = "/menu",
                Fields = new List<FormField>
                {
                    new() { Name = "titleKey", LabelKey = "menus.titleKey", Type = "text", Required = true },
                    new() { Name = "path", LabelKey = "menus.path", Type = "text" },
                    new() { Name = "icon", LabelKey = "menus.icon", Type = "text" },
                    new() { Name = "orderIndex", LabelKey = "menus.order", Type = "number", Required = true },
                    new() { Name = "parentId", LabelKey = "menus.parent", Type = "select", DataSourceEndpoint = "/lookup/menus" }
                }
            }
        };
    }

    private UiSchema GetUsersSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "sidebar.users",
                Endpoint = "/users",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "name", TitleKey = "users.name", Sortable = true },
                    new() { DataIndex = "email", TitleKey = "users.email" },
                    new() { DataIndex = "role", TitleKey = "users.role", Type = "tag" },
                    new() { DataIndex = "departmentName", TitleKey = "users.department" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/users/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/users/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "users.create",
                SubmitEndpoint = "/users",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "users.name", Type = "text", Required = true },
                    new() { Name = "email", LabelKey = "users.email", Type = "text", Required = true },
                    new() { Name = "role", LabelKey = "users.role", Type = "select", Options = new List<FieldOption> { new() { Label = "Requester", Value = "Requester" }, new() { Label = "Agent", Value = "Agent" }, new() { Label = "Approver", Value = "Approver" }, new() { Label = "Administrator", Value = "Administrator" } }, Required = true },
                    new() { Name = "departmentId", LabelKey = "users.department", Type = "select", DataSourceEndpoint = "/lookup/departments", Required = true }
                }
            }
        };
    }

    private UiSchema GetTemplatesSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "sidebar.templates",
                Endpoint = "/templates",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "serviceDeskName", TitleKey = "templates.serviceDesk" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/templates/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/templates/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "templates.create",
                SubmitEndpoint = "/templates",
                Fields = new List<FormField>
                {
                    new() { Name = "templateName", LabelKey = "templates.name", Type = "text", Required = true },
                    new() { Name = "serviceDeskId", LabelKey = "templates.serviceDesk", Type = "select", DataSourceEndpoint = "/servicedesks", Required = true },
                    new() { Name = "fields", LabelKey = "templates.fields", Type = "fields_builder", Required = true },
                    new() { Name = "workflow", LabelKey = "templates.workflow", Type = "workflow_builder", Required = true }
                }
            }
        };
    }

    private UiSchema GetCategoriesSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "sidebar.categories",
                Endpoint = "/categories",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "name", TitleKey = "categories.name", Sortable = true },
                    new() { DataIndex = "departmentName", TitleKey = "categories.department" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/categories/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/categories/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "categories.create",
                SubmitEndpoint = "/categories",
                Fields = new List<FormField>
                {
                    new() { Name = "departmentId", LabelKey = "categories.department", Type = "select", DataSourceEndpoint = "/lookup/departments", Required = true },
                    new() { Name = "parentCategoryId", LabelKey = "categories.parentCategory", Type = "select", DataSourceEndpoint = "/lookup/categories", Required = false }
                }
            }
        };
    }

    private UiSchema GetSlasSchema()
    {
        return new UiSchema
        {
            Grid = new GridSchema
            {
                TitleKey = "sidebar.slas",
                Endpoint = "/slaconfigurations",
                Actions = new List<string> { "add", "refresh" },
                Columns = new List<GridColumn>
                {
                    new() { DataIndex = "name", TitleKey = "slas.name", Sortable = true },
                    new() { DataIndex = "departmentName", TitleKey = "slas.department" },
                    new() { DataIndex = "priorityLevel", TitleKey = "slas.priority", Type = "tag" },
                    new() { DataIndex = "resolutionTimeMinutes", TitleKey = "slas.resolutionTime" },
                    new() { DataIndex = "firstResponseTimeMinutes", TitleKey = "slas.responseTime" }
                },
                RowActions = _userContext.Role == "Admin" ? new List<GridAction>
                {
                    new() { LabelKey = "actions.edit", Method = "PUT", Endpoint = "/slaconfigurations/{id}", Color = "primary", ActionType = "edit" },
                    new() { LabelKey = "actions.delete", Method = "DELETE", Endpoint = "/slaconfigurations/{id}", Color = "danger", ActionType = "delete" }
                } : new List<GridAction>()
            },
            Form = new FormSchema
            {
                TitleKey = "slas.create",
                SubmitEndpoint = "/slaconfigurations",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "slas.name", Type = "text", Required = true },
                    new() { Name = "departmentId", LabelKey = "slas.department", Type = "select", DataSourceEndpoint = "/lookup/departments", Required = true },
                    new() { Name = "priorityLevel", LabelKey = "slas.priority", Type = "select", Options = new List<FieldOption> { new() { Label = "Low", Value = "Low" }, new() { Label = "Medium", Value = "Medium" }, new() { Label = "High", Value = "High" }, new() { Label = "Critical", Value = "Critical" } }, Required = true },
                    new() { Name = "resolutionTimeMinutes", LabelKey = "slas.resolutionTime", Type = "number", Required = true },
                    new() { Name = "firstResponseTimeMinutes", LabelKey = "slas.responseTime", Type = "number", Required = true }
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
