using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
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

    public CreateLeaveRequestCommandHandler(
        IOmniFlowDbContext context,
        IUserContext userContext,
        ITenantService tenantService,
        MassTransit.IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _userContext = userContext;
        _tenantService = tenantService;
        _publishEndpoint = publishEndpoint;
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
