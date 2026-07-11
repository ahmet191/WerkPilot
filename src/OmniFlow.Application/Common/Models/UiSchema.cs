namespace OmniFlow.Application.Common.Models;

public class UiSchema
{
    public GridSchema Grid { get; set; } = new();
    public FormSchema Form { get; set; } = new();
}

public class GridSchema
{
    public string TitleKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public List<GridColumn> Columns { get; set; } = new();
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
    public List<FormField> Fields { get; set; } = new();
}

public class FormField
{
    public string Name { get; set; } = string.Empty;
    public string LabelKey { get; set; } = string.Empty;
    public string Type { get; set; } = "text"; // text, select, date, number
    public bool Required { get; set; } = false;
    public string? OptionsEndpoint { get; set; }
    // Hardcoded static options if no endpoint
    public List<FieldOption>? Options { get; set; }
}

public class FieldOption
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
