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
    private readonly ITenantService _tenantService;
    private readonly MassTransit.IPublishEndpoint _publishEndpoint;

    public AssignAssetCommandHandler(
        IOmniFlowDbContext context,
        ITenantService tenantService,
        MassTransit.IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _tenantService = tenantService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);
        if (asset == null)
            throw new OmniFlow.Domain.Common.Exceptions.NotFoundException(nameof(OmniFlow.Domain.Entities.Asset), request.AssetId);

        asset.AssignTo(request.UserId);

        // Publish BEFORE SaveChanges: the transactional outbox commits the
        // message atomically with the assignment.
        await _publishEndpoint.Publish(new OmniFlow.Domain.Events.AssetAssignedEvent
        {
            TenantId = _tenantService.TenantId,
            AssetId = asset.Id,
            AssetTag = asset.AssetTag,
            Model = asset.Model,
            AssignedToUserId = request.UserId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
