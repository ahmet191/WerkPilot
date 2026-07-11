using MediatR;
using Microsoft.EntityFrameworkCore;
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

public class GetMyLeaveRequestsQuery : IRequest<List<LeaveRequestDto>>
{
}

public class GetMyLeaveRequestsQueryHandler : IRequestHandler<GetMyLeaveRequestsQuery, List<LeaveRequestDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetMyLeaveRequestsQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<List<LeaveRequestDto>> Handle(GetMyLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var leaves = await _context.LeaveRequests
            .Where(l => l.UserId == _userContext.UserId)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync(cancellationToken);

        return leaves.Select(l => new LeaveRequestDto
        {
            Id = l.Id,
            Type = l.Type.ToString(),
            StartDate = l.StartDate,
            EndDate = l.EndDate,
            Status = l.Status.ToString(),
            Reason = l.Reason
        }).ToList();
    }
}
