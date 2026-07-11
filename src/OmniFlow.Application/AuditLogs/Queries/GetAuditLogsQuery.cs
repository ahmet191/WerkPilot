using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.AuditLogs.Queries;

public record GetAuditLogsQuery(PagedRequest PagedRequest) : IRequest<PagedResponse<AuditLogDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResponse<AuditLogDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetAuditLogsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs.AsNoTracking();

        // Apply filtering (example: by EntityType)
        if (request.PagedRequest.Filters.TryGetValue("entityType", out var entityType) && !string.IsNullOrEmpty(entityType))
        {
            query = query.Where(a => a.EntityType.Contains(entityType));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        if (!string.IsNullOrEmpty(request.PagedRequest.SortBy))
        {
            if (request.PagedRequest.SortBy.Equals("timestamp", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(a => a.Timestamp) : query.OrderBy(a => a.Timestamp);
            else if (request.PagedRequest.SortBy.Equals("action", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(a => a.Action) : query.OrderBy(a => a.Action);
            else if (request.PagedRequest.SortBy.Equals("entityType", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(a => a.EntityType) : query.OrderBy(a => a.EntityType);
            else
                query = query.OrderByDescending(a => a.Timestamp);
        }
        else
        {
            query = query.OrderByDescending(a => a.Timestamp);
        }

        // Apply pagination
        var items = await query
            .Skip((request.PagedRequest.PageNumber - 1) * request.PagedRequest.PageSize)
            .Take(request.PagedRequest.PageSize)
            .Select(al => new AuditLogDto
            {
                Id = al.Id,
                UserId = al.UserId,
                Action = al.Action,
                EntityType = al.EntityType,
                EntityId = al.EntityId,
                Details = al.Details,
                Timestamp = al.Timestamp
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<AuditLogDto>(items, totalCount, request.PagedRequest.PageNumber, request.PagedRequest.PageSize);
    }
}
