using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Approvals.Queries;

public class PendingApprovalDto
{
    public Guid Id { get; set; }               // ApprovalRequest id
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EntityDisplay { get; set; } = string.Empty;
    public string RequestedByName { get; set; } = string.Empty;
    public int StepOrder { get; set; }
    public int TotalSteps { get; set; }
    public string? ApproverRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Approval steps currently waiting on the calling user (directly,
/// via their role, via an active delegation, or everything for admins).</summary>
public record GetMyPendingApprovalsQuery : IRequest<List<PendingApprovalDto>>;

public class GetMyPendingApprovalsQueryHandler : IRequestHandler<GetMyPendingApprovalsQuery, List<PendingApprovalDto>>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public GetMyPendingApprovalsQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<List<PendingApprovalDto>> Handle(GetMyPendingApprovalsQuery request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        var role = _userContext.Role;
        var isAdmin = role == "Administrator";
        var now = DateTime.UtcNow;

        // Users who delegated their approval authority to me right now.
        var delegatorIds = await _context.AuthorityDelegations
            .Where(d => d.DelegateeId == userId && d.StartDate <= now && d.EndDate >= now)
            .Select(d => d.DelegatorId)
            .ToListAsync(cancellationToken);

        var query = _context.ApprovalSteps
            .Where(s => s.Status == ApprovalStatus.Pending
                        && s.ApprovalRequest.Status == ApprovalStatus.Pending
                        && s.Order == s.ApprovalRequest.CurrentStepOrder);

        if (!isAdmin)
        {
            query = query.Where(s =>
                (s.ApproverUserId != null && s.ApproverUserId == userId) ||
                (s.ApproverRole != null && s.ApproverRole == role) ||
                (s.ApproverUserId != null && delegatorIds.Contains(s.ApproverUserId.Value)));
        }

        var items = await query
            .OrderBy(s => s.ApprovalRequest.CreatedAt)
            .Select(s => new PendingApprovalDto
            {
                Id = s.ApprovalRequestId,
                EntityType = s.ApprovalRequest.EntityType,
                EntityId = s.ApprovalRequest.EntityId,
                EntityDisplay = s.ApprovalRequest.EntityDisplay,
                StepOrder = s.Order,
                TotalSteps = s.ApprovalRequest.Steps.Count,
                ApproverRole = s.ApproverRole,
                CreatedAt = s.ApprovalRequest.CreatedAt,
                RequestedByName = _context.Users
                    .Where(u => u.Id == s.ApprovalRequest.RequestedById)
                    .Select(u => u.Name)
                    .FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return items;
    }
}
