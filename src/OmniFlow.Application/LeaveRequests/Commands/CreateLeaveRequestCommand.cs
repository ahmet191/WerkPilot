using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Approvals;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Entities.Approvals;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.LeaveRequests.Commands;

public class CreateLeaveRequestCommand : IRequest<Guid>
{
    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class CreateLeaveRequestCommandHandler : IRequestHandler<CreateLeaveRequestCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;
    private readonly ITenantService _tenantService;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;
    private readonly ApprovalRequestBuilder _approvalBuilder;

    public CreateLeaveRequestCommandHandler(
        IOmniFlowDbContext context,
        IUserContext userContext,
        ITenantService tenantService,
        MassTransit.IPublishEndpoint publishEndpoint,
        ApprovalRequestBuilder approvalBuilder)
    {
        _context = context;
        _userContext = userContext;
        _tenantService = tenantService;
        _publishEndpoint = publishEndpoint;
        _approvalBuilder = approvalBuilder;
    }

    public async Task<Guid> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // Input validation (incl. date ordering) is enforced by
        // CreateLeaveRequestCommandValidator via the MediatR ValidationBehavior.
        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = _userContext.UserId,
            Type = request.Type,
            // Bare dates deserialize with Kind=Unspecified; Npgsql only accepts
            // UTC for 'timestamp with time zone' columns.
            StartDate = DateTime.SpecifyKind(request.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(request.EndDate, DateTimeKind.Utc),
            Status = LeaveStatus.Pending,
            Reason = request.Reason
        };

        _context.LeaveRequests.Add(leave);

        // Open the approval chain from the tenant's default leave policy
        // (seeded: manager → HRAdmin). Falls back to manager-only when the
        // tenant has no policy configured.
        var policy = await _context.Tenants
            .Where(t => t.Id == _tenantService.TenantId)
            .Select(t => t.LeaveApprovalPolicy)
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ApprovalDefinition
            {
                Steps = { new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.ManagerOfRequester } }
            };

        await _approvalBuilder.CreateAsync(
            ApprovalEntityTypes.LeaveRequest,
            leave.Id,
            $"{leave.Type} leave {leave.StartDate:yyyy-MM-dd} → {leave.EndDate:yyyy-MM-dd}",
            leave.UserId,
            policy,
            cancellationToken);

        // Publish BEFORE SaveChanges: the transactional outbox commits the
        // message atomically with the entity — nothing is lost on a crash.
        await _publishEndpoint.Publish(new OmniFlow.Domain.Events.LeaveRequestCreatedEvent
        {
            TenantId = _tenantService.TenantId,
            LeaveRequestId = leave.Id,
            RequesterId = leave.UserId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Type = leave.Type.ToString()
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return leave.Id;
    }
}
