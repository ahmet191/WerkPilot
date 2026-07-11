using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Assets.Commands;

public class AssignAssetCommand : IRequest<bool>
{
    public Guid AssetId { get; set; }
    public Guid UserId { get; set; }
}

public class AssignAssetCommandHandler : IRequestHandler<AssignAssetCommand, bool>
{
    private readonly IOmniFlowDbContext _context;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public AssignAssetCommandHandler(IOmniFlowDbContext context, MassTransit.IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);
        if (asset == null)
            throw new OmniFlow.Application.Common.Exceptions.ApiException("api.error.notfound");

        asset.AssignedToUserId = request.UserId;
        asset.Status = AssetStatus.Assigned;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish Event to MassTransit
        await _publishEndpoint.Publish(new OmniFlow.Domain.Events.AssetAssignedEvent
        {
            AssetId = asset.Id,
            AssetTag = asset.AssetTag,
            Model = asset.Model,
            AssignedToUserId = request.UserId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        return true;
    }
}
