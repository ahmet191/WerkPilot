using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
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

public record GetMyLeaveRequestsQuery(GridRequest Request) : IRequest<PagedResponse<LeaveRequestDto>>;

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
        if (string.IsNullOrEmpty(request.Request.SortBy))
        {
            request.Request.SortBy = nameof(LeaveRequestDto.StartDate);
            request.Request.SortDesc = true;
        }

        // Fully SQL-translated grid over the DTO projection.
        return await _context.LeaveRequests
            .AsNoTracking()
            .Where(l => l.UserId == _userContext.UserId)
            .Select(l => new LeaveRequestDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                Reason = l.Reason,
                Status = l.Status.ToString()
            })
            .ToGridResultAsync(request.Request, [nameof(LeaveRequestDto.Type), nameof(LeaveRequestDto.Status), nameof(LeaveRequestDto.Reason)], cancellationToken);
    }
}
