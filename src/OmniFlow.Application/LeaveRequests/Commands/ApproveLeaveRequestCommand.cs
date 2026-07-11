using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.LeaveRequests.Commands;

public class ApproveLeaveRequestCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public bool IsApproved { get; set; }
}

public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand, bool>
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public ApproveLeaveRequestCommandHandler(IOmniFlowDbContext context, IUserContext userContext, MassTransit.IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _userContext = userContext;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leave = await _context.LeaveRequests.FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);
        
        if (leave == null)
            throw new Exception("Leave request not found");

        leave.Status = request.IsApproved ? LeaveStatus.Approved : LeaveStatus.Rejected;
        leave.ApproverId = _userContext.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish Integration Event
        await _publishEndpoint.Publish(new OmniFlow.Domain.Events.LeaveRequestResolvedEvent
        {
            LeaveRequestId = leave.Id,
            RequesterId = leave.UserId,
            ApproverId = _userContext.UserId,
            IsApproved = request.IsApproved
        }, cancellationToken);

        return true;
    }
}
