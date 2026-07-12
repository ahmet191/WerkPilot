using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
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

public record GetAssetsQuery(GridRequest Request) : IRequest<PagedResponse<AssetDto>>;

public class GetAssetsQueryHandler : IRequestHandler<GetAssetsQuery, PagedResponse<AssetDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetAssetsQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AssetDto>> Handle(GetAssetsQuery request, CancellationToken cancellationToken)
    {
        // Fully SQL-translated grid over the DTO projection.
        return await _context.Assets
            .AsNoTracking()
            .Select(a => new AssetDto
            {
                Id = a.Id,
                AssetTag = a.AssetTag,
                SerialNumber = a.SerialNumber,
                Model = a.Model,
                Category = a.Category,
                Status = a.Status.ToString(),
                AssignedToUserId = a.AssignedToUserId,
                AssignedToUserName = a.AssignedToUser != null ? a.AssignedToUser.Name : string.Empty
            })
            .ToGridResultAsync(
                request.Request,
                [nameof(AssetDto.AssetTag), nameof(AssetDto.SerialNumber), nameof(AssetDto.Model), nameof(AssetDto.Category), nameof(AssetDto.AssignedToUserName)],
                cancellationToken);
    }
}
