namespace OmniFlow.Application.Common.Models;

/// <summary>
/// Standard server-side grid contract used by every list endpoint feeding the
/// frontend EnterpriseDataGrid. Binds from the query string:
/// <c>?current=1&amp;pageSize=20&amp;search=foo&amp;sortBy=name&amp;sortDesc=true&amp;filters[status]=Assigned</c>
/// </summary>
public class GridRequest
{
    private const int MaxPageSize = 200;

    private int _pageSize = 20;

    /// <summary>1-based page number (AntD pagination naming).</summary>
    public int Current { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    /// <summary>Free-text search applied across the endpoint's searchable columns.</summary>
    public string? Search { get; set; }

    /// <summary>Column (camelCase dataIndex) to sort by; resolved case-insensitively.</summary>
    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }

    /// <summary>Per-column filter values, keyed by column dataIndex.</summary>
    public Dictionary<string, string> Filters { get; set; } = new();
}
