using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public CategoriesController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var lang = _userContext.Language;
        var categories = await _context.TicketCategories
            .Include(c => c.Department)
            .Select(c => new
            {
                c.Id,
                Name = c.LocalizedName.Translations.ContainsKey(lang) ? c.LocalizedName.Translations[lang] : (c.LocalizedName.Translations.ContainsKey("en") ? c.LocalizedName.Translations["en"] : "Unknown"),
                c.DepartmentId,
                DepartmentName = c.Department != null ? c.Department.LocalizedName.Translations.ContainsKey(lang) ? c.Department.LocalizedName.Translations[lang] : (c.Department.LocalizedName.Translations.ContainsKey("en") ? c.Department.LocalizedName.Translations["en"] : "Unknown") : "None",
                c.ParentCategoryId
            })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TicketCategory category)
    {
        if (category.Id == Guid.Empty)
            category.Id = Guid.NewGuid();

        _context.TicketCategories.Add(category);
        await _context.SaveChangesAsync(default);

        return Ok(category);
    }
}
