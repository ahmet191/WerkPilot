using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;

namespace OmniFlow.Application.UnitTests.Grid;

public class GridQueryExtensionsTests
{
    private sealed class Person
    {
        public string Name { get; init; } = string.Empty;
        public string Department { get; init; } = string.Empty;
        public int Age { get; init; }
    }

    private static readonly List<Person> People =
    [
        new() { Name = "Alice", Department = "HR", Age = 30 },
        new() { Name = "Bob", Department = "IT", Age = 25 },
        new() { Name = "Carol", Department = "IT", Age = 40 },
        new() { Name = "Dave", Department = "Finance", Age = 35 },
    ];

    [Fact]
    public void Unfiltered_returns_all_with_total()
    {
        var result = People.ToGridResult(new GridRequest { Current = 1, PageSize = 10 });
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(4, result.Items.Count);
    }

    [Fact]
    public void Search_matches_across_string_fields_case_insensitively()
    {
        var result = People.ToGridResult(
            new GridRequest { Search = "it" },
            ["Name", "Department"]);

        // "IT" department matches Bob and Carol.
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, p => Assert.Equal("IT", p.Department));
    }

    [Fact]
    public void Column_filter_narrows_rows()
    {
        var request = new GridRequest();
        request.Filters["Department"] = "Finance";

        var result = People.ToGridResult(request);
        Assert.Single(result.Items);
        Assert.Equal("Dave", result.Items[0].Name);
    }

    [Fact]
    public void Sort_descending_orders_correctly()
    {
        var result = People.ToGridResult(new GridRequest { SortBy = "Age", SortDesc = true });
        Assert.Equal(new[] { 40, 35, 30, 25 }, result.Items.Select(p => p.Age).ToArray());
    }

    [Fact]
    public void Paging_returns_requested_slice_but_full_total()
    {
        var result = People.ToGridResult(new GridRequest { Current = 2, PageSize = 2, SortBy = "Name" });
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        // Sorted by name: [Alice, Bob, Carol, Dave] → page 2 = [Carol, Dave]
        Assert.Equal(new[] { "Carol", "Dave" }, result.Items.Select(p => p.Name).ToArray());
    }

    [Fact]
    public void Unknown_sort_column_is_ignored_not_thrown()
    {
        var result = People.ToGridResult(new GridRequest { SortBy = "DoesNotExist" });
        Assert.Equal(4, result.TotalCount);
    }
}
