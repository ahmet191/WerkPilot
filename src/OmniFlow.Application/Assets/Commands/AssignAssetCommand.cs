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

    public AssignAssetCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(AssignAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == request.AssetId, cancellationToken);
        if (asset == null)
            throw new Exception("Asset not found");

        asset.AssignedToUserId = request.UserId;
        asset.Status = AssetStatus.Assigned;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
