using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;

    public MenuController(IOmniFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenu()
    {
        // Simple logic: fetch all active menus, ordered by index.
        // We do not eagerly load children recursively in the DB query,
        // instead we fetch all active and build the tree in memory for simplicity.
        var allMenus = await _context.AppMenus
            .Where(m => m.IsActive)
            .OrderBy(m => m.OrderIndex)
            .ToListAsync();

        var roots = allMenus.Where(m => m.ParentId == null).ToList();

        var result = roots.Select(root => BuildMenuDto(root, allMenus)).ToList();

        return Ok(result);
    }

    private MenuDto BuildMenuDto(AppMenu menu, List<AppMenu> allMenus)
    {
        var children = allMenus.Where(m => m.ParentId == menu.Id)
                               .OrderBy(m => m.OrderIndex)
                               .Select(child => BuildMenuDto(child, allMenus))
                               .ToList();

        return new MenuDto
        {
            Id = menu.Id,
            TitleKey = menu.TitleKey,
            Path = menu.Path,
            Icon = menu.Icon,
            Children = children.Any() ? children : null
        };
    }
}

public class MenuDto
{
    public Guid Id { get; set; }
    public string TitleKey { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public List<MenuDto>? Children { get; set; }
}
