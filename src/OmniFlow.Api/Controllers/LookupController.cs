using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/lookup")]
public class LookupController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public LookupController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _context.Users
            .Select(u => new { label = u.Name, value = u.Id })
            .ToListAsync();
        return Ok(users);
    }
    
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        var lang = _userContext.Language;
        var departments = await _context.Departments.ToListAsync();
        var lookup = departments
            .Select(d => new { label = d.LocalizedName.Translations.ContainsKey(lang) ? d.LocalizedName.Translations[lang] : (d.LocalizedName.Translations.ContainsKey("en") ? d.LocalizedName.Translations["en"] : "Unknown"), value = d.Id })
            .ToList();
        return Ok(lookup);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var lang = _userContext.Language;
        var categories = await _context.TicketCategories.ToListAsync();
        var lookup = categories
            .Select(c => new { label = c.LocalizedName.Translations.ContainsKey(lang) ? c.LocalizedName.Translations[lang] : (c.LocalizedName.Translations.ContainsKey("en") ? c.LocalizedName.Translations["en"] : "Unknown"), value = c.Id })
            .ToList();
        return Ok(lookup);
    }

    [HttpGet("menus")]
    public async Task<IActionResult> GetMenus()
    {
        var menus = await _context.AppMenus
            .Select(m => new { label = m.TitleKey, value = m.Id })
            .ToListAsync();
        return Ok(menus);
    }

    [HttpGet("leavetypes")]
    public IActionResult GetLeaveTypes()
    {
        var types = Enum.GetValues(typeof(OmniFlow.Domain.Enums.LeaveType))
            .Cast<OmniFlow.Domain.Enums.LeaveType>()
            .Select(lt => new { label = lt.ToString(), value = (int)lt })
            .ToList();
        return Ok(types);
    }
}
