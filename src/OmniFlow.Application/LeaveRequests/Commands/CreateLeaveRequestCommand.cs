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
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public CreateLeaveRequestCommandHandler(IOmniFlowDbContext context, IUserContext userContext, MassTransit.IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _userContext = userContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Guid> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        // Simple business logic check
        if (request.EndDate <= request.StartDate)
            throw new Exception("End Date must be after Start Date.");

        var leave = new LeaveRequest
        {
            Id = Guid.NewGuid(),
            UserId = _userContext.UserId,
            Type = request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = LeaveStatus.Pending,
            Reason = request.Reason
        };

        _context.LeaveRequests.Add(leave);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish Integration Event
        await _publishEndpoint.Publish(new OmniFlow.Domain.Events.LeaveRequestCreatedEvent
        {
            LeaveRequestId = leave.Id,
            RequesterId = leave.UserId,
            StartDate = leave.StartDate,
            EndDate = leave.EndDate,
            Type = leave.Type.ToString()
        }, cancellationToken);

        return leave.Id;
    }
}
