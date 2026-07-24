using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Approvals.Queries;

public class ApprovalTimelineDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CurrentStepOrder { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<ApprovalTimelineStepDto> Steps { get; set; } = new();
}

public class ApprovalTimelineStepDto
{
    public int Order { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverRole { get; set; }
    public string? ApproverName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ActedByName { get; set; }
    public DateTimeOffset? ActedAt { get; set; }
    public string? Comment { get; set; }
}

/// <summary>Full approval history of a business entity (newest first) for
/// timeline rendering in detail views.</summary>
public record GetApprovalsForEntityQuery(string EntityType, Guid EntityId) : IRequest<List<ApprovalTimelineDto>>;

public class GetApprovalsForEntityQueryHandler : IRequestHandler<GetApprovalsForEntityQuery, List<ApprovalTimelineDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetApprovalsForEntityQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<ApprovalTimelineDto>> Handle(GetApprovalsForEntityQuery request, CancellationToken cancellationToken)
    {
        var approvals = await _context.ApprovalRequests
            .AsNoTracking()
            .Include(a => a.Steps)
            .Where(a => a.EntityType == request.EntityType && a.EntityId == request.EntityId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        if (approvals.Count == 0) return new List<ApprovalTimelineDto>();

        // Resolve display names in one round trip.
        var userIds = new HashSet<Guid>();
        foreach (var approval in approvals)
        {
            userIds.Add(approval.RequestedById);
            foreach (var step in approval.Steps)
            {
                if (step.ApproverUserId.HasValue) userIds.Add(step.ApproverUserId.Value);
                if (step.ActedById.HasValue) userIds.Add(step.ActedById.Value);
            }
        }

        var names = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        return approvals.Select(a => new ApprovalTimelineDto
        {
            Id = a.Id,
            Status = a.Status.ToString(),
            CurrentStepOrder = a.CurrentStepOrder,
            CreatedAt = a.CreatedAt,
            CompletedAt = a.CompletedAt,
            RequestedByName = names.GetValueOrDefault(a.RequestedById, string.Empty),
            Steps = a.Steps.OrderBy(s => s.Order).Select(s => new ApprovalTimelineStepDto
            {
                Order = s.Order,
                ApproverType = s.ApproverType.ToString(),
                ApproverRole = s.ApproverRole,
                ApproverName = s.ApproverUserId.HasValue ? names.GetValueOrDefault(s.ApproverUserId.Value) : null,
                Status = s.Status.ToString(),
                ActedByName = s.ActedById.HasValue ? names.GetValueOrDefault(s.ActedById.Value) : null,
                ActedAt = s.ActedAt,
                Comment = s.Comment
            }).ToList()
        }).ToList();
    }
}
