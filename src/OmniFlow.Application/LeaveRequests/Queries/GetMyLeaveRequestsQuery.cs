using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.LeaveRequests.Queries;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

public record GetMyLeaveRequestsQuery(PagedRequest PagedRequest) : IRequest<PagedResponse<LeaveRequestDto>>;

public class GetMyLeaveRequestsQueryHandler : IRequestHandler<GetMyLeaveRequestsQuery, PagedResponse<LeaveRequestDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetMyLeaveRequestsQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<PagedResponse<LeaveRequestDto>> Handle(GetMyLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.UserId == _userContext.UserId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        if (!string.IsNullOrEmpty(request.PagedRequest.SortBy))
        {
            if (request.PagedRequest.SortBy.Equals("startDate", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(l => l.StartDate) : query.OrderBy(l => l.StartDate);
            else if (request.PagedRequest.SortBy.Equals("type", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(l => l.Type) : query.OrderBy(l => l.Type);
            else if (request.PagedRequest.SortBy.Equals("status", StringComparison.OrdinalIgnoreCase))
                query = request.PagedRequest.SortDesc ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status);
            else
                query = query.OrderByDescending(l => l.StartDate);
        }
        else
        {
            query = query.OrderByDescending(l => l.StartDate);
        }

        var items = await query
            .Skip((request.PagedRequest.PageNumber - 1) * request.PagedRequest.PageSize)
            .Take(request.PagedRequest.PageSize)
            .Select(l => new LeaveRequestDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Reason = l.Reason,
                Status = l.Status.ToString()
            })
            .ToListAsync(cancellationToken);

        return new PagedResponse<LeaveRequestDto>(items, totalCount, request.PagedRequest.PageNumber, request.PagedRequest.PageSize);
    }
}
