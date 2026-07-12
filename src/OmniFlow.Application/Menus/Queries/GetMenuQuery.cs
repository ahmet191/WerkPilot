using MediatR;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Application.Menus.Queries;

public class MenuDto
{
    public Guid Id { get; set; }
    public string TitleKey { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public List<MenuDto>? Children { get; set; }
}

public record GetMenuQuery : IRequest<List<MenuDto>>;

public class GetMenuQueryHandler : IRequestHandler<GetMenuQuery, List<MenuDto>>
{
    private readonly IOmniFlowDbContext _context;

    public GetMenuQueryHandler(IOmniFlowDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuDto>> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        // Fetch all active menus once and build the tree in memory.
        var allMenus = await _context.AppMenus
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync(cancellationToken);

        return allMenus
            .Where(m => m.ParentId == null)
            .Select(root => BuildMenuDto(root, allMenus))
            .ToList();
    }

    private static MenuDto BuildMenuDto(AppMenu menu, List<AppMenu> allMenus)
    {
        var children = allMenus
            .Where(m => m.ParentId == menu.Id)
            .OrderBy(m => m.OrderIndex)
            .Select(child => BuildMenuDto(child, allMenus))
            .ToList();

        return new MenuDto
        {
            Id = menu.Id,
            TitleKey = menu.TitleKey,
            Path = menu.Path,
            Icon = menu.Icon,
            Children = children.Count != 0 ? children : null
        };
    }
}
