using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Models;

namespace OmniFlow.Application.Common.Grid;

/// <summary>
/// Server-side grid engine. Applies free-text search, per-column filters,
/// dynamic sorting and paging described by a <see cref="GridRequest"/>.
///
/// Two execution modes:
/// <list type="bullet">
/// <item><see cref="ToGridResultAsync{T}"/> — composes expression trees over an
/// <see cref="IQueryable{T}"/> so everything runs in SQL. Use for high-volume
/// tables (tickets, audit logs, expenses).</item>
/// <item><see cref="ToGridResult{T}"/> — reflection-based, for endpoints that
/// must materialize first (localized JSONB projections). Still server-side:
/// only one page ever crosses the wire.</item>
/// </list>
/// Unknown columns are ignored rather than throwing, so a stale frontend
/// schema can never break an endpoint.
/// </summary>
public static class GridQueryExtensions
{
    // ───────────────────────── IQueryable (SQL-translated) ─────────────────────────

    public static async Task<PagedResponse<T>> ToGridResultAsync<T>(
        this IQueryable<T> query,
        GridRequest request,
        string[]? searchFields = null,
        CancellationToken cancellationToken = default)
    {
        query = query.ApplySearch(request.Search, searchFields);
        query = query.ApplyFilters(request.Filters);
        query = query.ApplySort(request.SortBy, request.SortDesc);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Current - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, total, request.Current, request.PageSize);
    }

    private static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string? search, string[]? searchFields)
    {
        if (string.IsNullOrWhiteSpace(search)) return query;

        var props = ResolveStringProperties<T>(searchFields);
        if (props.Count == 0) return query;

        var param = Expression.Parameter(typeof(T), "e");
        var term = Expression.Constant(search.Trim().ToLower());

        Expression? body = null;
        foreach (var prop in props)
        {
            var contains = BuildStringContains(param, prop, term);
            body = body is null ? contains : Expression.OrElse(body, contains);
        }

        return body is null ? query : query.Where(Expression.Lambda<Func<T, bool>>(body, param));
    }

    private static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, Dictionary<string, string> filters)
    {
        foreach (var (field, value) in filters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var prop = ResolveProperty<T>(field);
            if (prop is null) continue;

            var param = Expression.Parameter(typeof(T), "e");
            var predicate = BuildFilterPredicate(param, prop, value);
            if (predicate is null) continue;

            query = query.Where(Expression.Lambda<Func<T, bool>>(predicate, param));
        }

        return query;
    }

    private static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string? sortBy, bool desc)
    {
        if (string.IsNullOrWhiteSpace(sortBy)) return query;

        var prop = ResolveProperty<T>(sortBy);
        if (prop is null) return query;

        var param = Expression.Parameter(typeof(T), "e");
        var lambda = Expression.Lambda(Expression.Property(param, prop), param);

        var call = Expression.Call(
            typeof(Queryable),
            desc ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy),
            [typeof(T), prop.PropertyType],
            query.Expression,
            Expression.Quote(lambda));

        return query.Provider.CreateQuery<T>(call);
    }

    /// <summary>e.Prop != null &amp;&amp; e.Prop.ToLower().Contains(term)</summary>
    private static Expression BuildStringContains(ParameterExpression param, PropertyInfo prop, Expression loweredTerm)
    {
        var access = Expression.Property(param, prop);
        var notNull = Expression.NotEqual(access, Expression.Constant(null, typeof(string)));
        var toLower = Expression.Call(access, nameof(string.ToLower), Type.EmptyTypes);
        var contains = Expression.Call(toLower, nameof(string.Contains), Type.EmptyTypes, loweredTerm);
        return Expression.AndAlso(notNull, contains);
    }

    /// <summary>
    /// Strings filter by case-insensitive contains; enums, GUIDs, booleans,
    /// numbers and dates by exact match. Unparseable values yield no predicate.
    /// </summary>
    private static Expression? BuildFilterPredicate(ParameterExpression param, PropertyInfo prop, string value)
    {
        var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        if (propType == typeof(string))
        {
            return BuildStringContains(param, prop, Expression.Constant(value.Trim().ToLower()));
        }

        object? parsed = TryParseValue(propType, value);
        if (parsed is null) return null;

        var access = Expression.Property(param, prop);
        var constant = Expression.Constant(parsed, propType);

        // Lift the constant for nullable property types so EF builds valid SQL.
        return Expression.Equal(access, Expression.Convert(constant, prop.PropertyType));
    }

    private static object? TryParseValue(Type type, string value)
    {
        try
        {
            if (type.IsEnum) return Enum.Parse(type, value, ignoreCase: true);
            if (type == typeof(Guid)) return Guid.Parse(value);
            if (type == typeof(bool)) return bool.Parse(value);
            if (type == typeof(int)) return int.Parse(value);
            if (type == typeof(long)) return long.Parse(value);
            if (type == typeof(decimal)) return decimal.Parse(value);
            if (type == typeof(double)) return double.Parse(value);
            if (type == typeof(DateTime)) return DateTime.Parse(value).ToUniversalTime();
            if (type == typeof(DateTimeOffset)) return DateTimeOffset.Parse(value).ToUniversalTime();
        }
        catch
        {
            // Filter value not parseable for this column type — ignore the filter.
        }

        return null;
    }

    // ───────────────────────── IEnumerable (materialized projections) ─────────────────────────

    public static PagedResponse<T> ToGridResult<T>(
        this IEnumerable<T> source,
        GridRequest request,
        string[]? searchFields = null)
    {
        var items = source;

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var props = ResolveStringProperties<T>(searchFields);
            var term = request.Search.Trim();
            items = items.Where(item => props.Any(p =>
                p.GetValue(item) is string s && s.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var (field, value) in request.Filters)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;

            var prop = ResolveProperty<T>(field);
            if (prop is null) continue;

            // In-memory shapes are display projections — compare everything as text.
            items = items.Where(item =>
                prop.GetValue(item)?.ToString()?.Contains(value, StringComparison.OrdinalIgnoreCase) == true);
        }

        if (!string.IsNullOrWhiteSpace(request.SortBy) && ResolveProperty<T>(request.SortBy) is { } sortProp)
        {
            items = request.SortDesc
                ? items.OrderByDescending(i => sortProp.GetValue(i), NaturalComparer.Instance)
                : items.OrderBy(i => sortProp.GetValue(i), NaturalComparer.Instance);
        }

        var list = items.ToList();
        var page = list
            .Skip((request.Current - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResponse<T>(page, list.Count, request.Current, request.PageSize);
    }

    // ───────────────────────── shared helpers ─────────────────────────

    private static PropertyInfo? ResolveProperty<T>(string name) =>
        typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    private static List<PropertyInfo> ResolveStringProperties<T>(string[]? fields)
    {
        var all = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string));

        if (fields is { Length: > 0 })
        {
            var wanted = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
            all = all.Where(p => wanted.Contains(p.Name));
        }

        return all.ToList();
    }

    /// <summary>Compares boxed values of mixed nullability without throwing.</summary>
    private sealed class NaturalComparer : IComparer<object?>
    {
        public static readonly NaturalComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return -1;
            if (y is null) return 1;
            if (x is IComparable cx && x.GetType() == y.GetType()) return cx.CompareTo(y);
            return string.Compare(x.ToString(), y.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
