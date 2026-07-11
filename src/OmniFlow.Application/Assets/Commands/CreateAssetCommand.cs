using MediatR;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Application.Assets.Commands;

public class CreateAssetCommand : IRequest<Guid>
{
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class CreateAssetCommandHandler : IRequestHandler<CreateAssetCommand, Guid>
{
    private readonly IOmniFlowDbContext _context;

    public CreateAssetCommandHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = new Asset
        {
            Id = Guid.NewGuid(),
            AssetTag = request.AssetTag,
            SerialNumber = request.SerialNumber,
            Model = request.Model,
            Category = request.Category,
            Status = AssetStatus.InStock
        };

        _context.Assets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);

        return asset.Id;
    }
}
