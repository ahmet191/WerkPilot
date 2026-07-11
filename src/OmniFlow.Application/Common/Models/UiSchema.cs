namespace OmniFlow.Application.Common.Models;

public class UiSchema
{
    public GridSchema Grid { get; set; } = new();
    public FormSchema Form { get; set; } = new();
    public List<WidgetSchema> Widgets { get; set; } = new();
}

public class WidgetSchema
{
    public string Type { get; set; } = string.Empty; // e.g. "statistic"
    public string TitleKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string DataKey { get; set; } = string.Empty; // the json key from endpoint
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

public class GridSchema
{
    public string TitleKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<GridColumn> Columns { get; set; } = new();
    public List<string> Actions { get; set; } = new();
    public List<GridAction> RowActions { get; set; } = new();
}

public class GridAction
{
    public string LabelKey { get; set; } = string.Empty;
    public string Method { get; set; } = "POST"; // POST, PUT, DELETE
    public string Endpoint { get; set; } = string.Empty; // e.g. /api/leaverequests/{id}/approve
    public string Color { get; set; } = "default"; // primary, dashed, link, text, default, danger, success
}

public class GridColumn
{
    public string DataIndex { get; set; } = string.Empty;
    public string TitleKey { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, number, date, tag
    public bool Sortable { get; set; } = false;
    public bool Ellipsis { get; set; } = false;
    public int? Width { get; set; }
}

public class FormSchema
{
    public string TitleKey { get; set; } = string.Empty;
    public string SubmitEndpoint { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public List<FormField> Fields { get; set; } = new();
    
    // For template-driven dynamic forms
    public string? TemplateEndpoint { get; set; } // e.g., "/api/templates"
    public string? DynamicSchemaEndpoint { get; set; } // e.g., "/api/uischema/forms/tickets/{templateId}"
}

public class FormField
{
    public string Name { get; set; } = string.Empty;
    public string LabelKey { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // e.g., text, select, date
    public bool Required { get; set; }
    
    // Advanced Form Validations & Relationships
    public string DataSourceEndpoint { get; set; } = string.Empty; // For 'select' types, e.g. /api/lookup/users
    public string RegexPattern { get; set; } = string.Empty;
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public string? OptionsEndpoint { get; set; }
    // Hardcoded static options if no endpoint
    public List<FieldOption>? Options { get; set; }
}

public class FieldOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
