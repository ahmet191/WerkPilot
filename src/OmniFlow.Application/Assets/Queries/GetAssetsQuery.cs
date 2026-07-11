using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Application.Assets.Queries;

public class AssetDto
{
    public Guid Id { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? AssignedToUserId { get; set; }
    public string AssignedToUserName { get; set; } = string.Empty;
}

public class GetAssetsQuery : IRequest<List<AssetDto>>
{
}

public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, List<AssetDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetAssetsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        var assets = await _context.Assets
            .Include(a => a.AssignedToUser)
            .ToListAsync(cancellationToken);
        
        return assets.Select(a => new AssetDto
        {
            Id = a.Id,
            AssetTag = a.AssetTag,
            SerialNumber = a.SerialNumber,
            Model = a.Model,
            Category = a.Category,
            Status = a.Status.ToString(),
            AssignedToUserId = a.AssignedToUserId,
            AssignedToUserName = a.AssignedToUser != null ? a.AssignedToUser.Name : string.Empty
        }).ToList();
    }
}
