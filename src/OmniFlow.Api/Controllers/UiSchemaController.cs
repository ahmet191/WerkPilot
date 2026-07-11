using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Common.Models;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UiSchemaController : ControllerBase
{
    [HttpGet("{entityType}")]
    public IActionResult GetSchema(string entityType)
    {
        var schema = entityType.ToLower() switch
        {
            "departments" => GetDepartmentsSchema(),
            "assets" => GetAssetsSchema(),
            "tickets" => GetTicketsSchema(),
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
                TitleKey = "departments.title",
                SubmitEndpoint = "/departments",
                Fields = new List<FormField>
                {
                    new() { Name = "name", LabelKey = "departments.name", Type = "text", Required = true },
                    new() { Name = "managerId", LabelKey = "departments.manager", Type = "text" }, // In a real app, 'select' with OptionsEndpoint = "/users"
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
                TitleKey = "assets.title",
                SubmitEndpoint = "/assets",
                Fields = new List<FormField>
                {
                    new() { Name = "assetTag", LabelKey = "assets.tag", Type = "text", Required = true },
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
                Fields = new List<FormField>
                {
                    new() { Name = "templateId", LabelKey = "tickets.templateId", Type = "text", Required = true },
                    new() { Name = "requesterId", LabelKey = "tickets.requesterId", Type = "text", Required = true }
                    // DynamicData would normally be nested or fetched per template
                }
            }
        };
    }
}
