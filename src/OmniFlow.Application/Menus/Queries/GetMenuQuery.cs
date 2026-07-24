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
    private readonly IUserContext _userContext;

    public GetMenuQueryHandler(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    public async Task<List<MenuDto>> Handle(GetMenuQuery request, CancellationToken cancellationToken)
    {
        // Fetch all active menus once and build the tree in memory.
        var allMenus = await _context.AppMenus
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync(cancellationToken);

        // Hide sections the caller's roles don't grant. Filtering here (rather than
        // shipping RequiredRoles to the client) keeps the admin/HR/IT structure off
        // the wire for users who can't reach it.
        var roles = _userContext.Roles;
        var visible = allMenus.Where(m => CanSee(m, roles)).ToList();

        return visible
            .Where(m => m.ParentId == null)
            .Select(root => BuildMenuDto(root, visible))
            .ToList();
    }

    private static bool CanSee(AppMenu menu, IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(menu.RequiredRoles)) return true;
        // Administrator is a super-role: it always sees everything.
        if (roles.Contains("Administrator", StringComparer.OrdinalIgnoreCase)) return true;
        var required = menu.RequiredRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return required.Any(r => roles.Contains(r, StringComparer.OrdinalIgnoreCase));
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
