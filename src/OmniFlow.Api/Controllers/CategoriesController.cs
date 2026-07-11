using Microsoft.AspNetCore.Authorization;
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
            .ToListAsync();

        var result = categories.Select(c => new
            {
                c.Id,
                Name = c.LocalizedName.Translations.ContainsKey(lang) ? c.LocalizedName.Translations[lang] : (c.LocalizedName.Translations.ContainsKey("en") ? c.LocalizedName.Translations["en"] : "Unknown"),
                c.DepartmentId,
                DepartmentName = c.Department != null ? c.Department.LocalizedName.Translations.ContainsKey(lang) ? c.Department.LocalizedName.Translations[lang] : (c.Department.LocalizedName.Translations.ContainsKey("en") ? c.Department.LocalizedName.Translations["en"] : "Unknown") : "None",
                c.ParentCategoryId
            })
            .ToList();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Create([FromBody] CategoryRequest request)
    {
        var category = new TicketCategory
        {
            Id = Guid.NewGuid(),
            LocalizedName = new Domain.Common.MultiLingualString
            {
                Translations = new Dictionary<string, string>
                {
                    { "en", request.Name },
                    { "tr", request.Name }
                }
            },
            DepartmentId = request.DepartmentId
        };

        _context.TicketCategories.Add(category);
        await _context.SaveChangesAsync(default);

        return Ok(category);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CategoryRequest request)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category == null) return NotFound();

        if (category.LocalizedName != null && category.LocalizedName.Translations != null)
        {
            category.LocalizedName.Translations["en"] = request.Name;
            category.LocalizedName.Translations["tr"] = request.Name;
        }
        else
        {
            category.LocalizedName = new Domain.Common.MultiLingualString
            {
                Translations = new Dictionary<string, string>
                {
                    { "en", request.Name },
                    { "tr", request.Name }
                }
            };
        }

        category.DepartmentId = request.DepartmentId;
        
        await _context.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.TicketCategories.FindAsync(id);
        if (category == null) return NotFound();

        _context.TicketCategories.Remove(category);
        await _context.SaveChangesAsync(default);
        return NoContent();
    }
}

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
}
