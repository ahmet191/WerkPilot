using MediatR;
using OmniFlow.Application.Common.Exceptions;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Assets.Commands;

public class UpdateAssetCommand : IRequest
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class UpdateAssetCommandHandler : IRequestHandler<UpdateAssetCommand>
{
    private readonly IOmniFlowDbContext _context;

    public UpdateAssetCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await _context.Assets.FindAsync(new object[] { request.Id }, cancellationToken);
        if (asset == null) throw new NotFoundException("Asset", request.Id);

        asset.AssetTag = request.AssetTag;
        asset.SerialNumber = request.SerialNumber;
        asset.Model = request.Model;
        asset.Category = request.Category;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
