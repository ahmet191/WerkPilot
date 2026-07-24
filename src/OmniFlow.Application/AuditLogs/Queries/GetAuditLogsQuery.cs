using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.AuditLogs.Queries;

public record GetAuditLogsQuery(GridRequest Request) : IRequest<PagedResponse<AuditLogDto>>;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResponse<AuditLogDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetAuditLogsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Newest first unless the grid requests an explicit sort.
        if (string.IsNullOrEmpty(request.Request.SortBy))
        {
            request.Request.SortBy = nameof(AuditLogDto.Timestamp);
            request.Request.SortDesc = true;
        }

        // Fully SQL-translated: search/filter/sort/paging run in Postgres —
        // audit logs are the highest-volume table in the system.
        return await _context.AuditLogs
            .AsNoTracking()
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
            .ToGridResultAsync(request.Request, [nameof(AuditLogDto.Action), nameof(AuditLogDto.EntityType), nameof(AuditLogDto.Details)], cancellationToken);
    }
}
