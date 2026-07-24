namespace OmniFlow.Application.Common.Models;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = false;
    
    // Key: Field name, Value: Filter text
    public Dictionary<string, string> Filters { get; set; } = new();
}
