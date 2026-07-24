using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using System.Text.Json;

namespace OmniFlow.Application.Admin.Queries;

public record SystemLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? Exception,
    string? TraceId,
    Guid? UserId,
    Dictionary<string, object> Properties);

public record GetSystemLogsQuery(
    int PageIndex = 0,
    int PageSize = 50,
    string? Level = null,
    string? TraceId = null,
    DateTimeOffset? StartDate = null,
    DateTimeOffset? EndDate = null) : IRequest<(IReadOnlyList<SystemLogDto> Items, int TotalCount)>;

public class GetSystemLogsValidator : AbstractValidator<GetSystemLogsQuery>
{
    public GetSystemLogsValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(10, 500);
        RuleFor(x => x.PageIndex).GreaterThanOrEqualTo(0);
    }
}

public class GetSystemLogsHandler : IRequestHandler<GetSystemLogsQuery, (IReadOnlyList<SystemLogDto> Items, int TotalCount)>
{
    private readonly IOmniFlowDbContext _context;

    public GetSystemLogsHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<SystemLogDto> Items, int TotalCount)> Handle(GetSystemLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.SystemLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Level))
            query = query.Where(l => l.Level == request.Level);

        if (!string.IsNullOrWhiteSpace(request.TraceId))
            query = query.Where(l => l.TraceId == request.TraceId);

        if (request.StartDate.HasValue)
            query = query.Where(l => l.Timestamp >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(l => l.Timestamp <= request.EndDate.Value);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .Select(l => new SystemLogDto(
                l.Id,
                l.Timestamp,
                l.Level,
                l.Message,
                l.Exception,
                l.TraceId,
                l.UserId,
                JsonSerializer.Deserialize<Dictionary<string, object>>(l.Properties, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            ))
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}

public record GetTraceDetailsQuery(string TraceId) : IRequest<IReadOnlyList<SystemLogDto>>;

public class GetTraceDetailsHandler : IRequestHandler<GetTraceDetailsQuery, IReadOnlyList<SystemLogDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetTraceDetailsHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SystemLogDto>> Handle(GetTraceDetailsQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.SystemLogs
            .AsNoTracking()
            .Where(l => l.TraceId == request.TraceId)
            .OrderBy(l => l.Timestamp)
            .Select(l => new SystemLogDto(
                l.Id,
                l.Timestamp,
                l.Level,
                l.Message,
                l.Exception,
                l.TraceId,
                l.UserId,
                JsonSerializer.Deserialize<Dictionary<string, object>>(l.Properties, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
            ))
            .ToListAsync(cancellationToken);

        return items;
    }
}
