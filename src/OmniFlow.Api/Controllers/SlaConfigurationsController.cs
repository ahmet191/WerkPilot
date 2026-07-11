using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SlaConfigurationsController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public SlaConfigurationsController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var lang = _userContext.Language;
        var slas = await _context.SLAConfigurations
            .Include(s => s.Department)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.PriorityLevel,
                s.ResolutionTimeMinutes,
                s.FirstResponseTimeMinutes,
                s.DepartmentId,
                DepartmentName = s.Department != null ? s.Department.LocalizedName.Translations.ContainsKey(lang) ? s.Department.LocalizedName.Translations[lang] : (s.Department.LocalizedName.Translations.ContainsKey("en") ? s.Department.LocalizedName.Translations["en"] : "Unknown") : "None"
            })
            .ToListAsync();

        return Ok(slas);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SLAConfiguration sla)
    {
        if (sla.Id == Guid.Empty)
            sla.Id = Guid.NewGuid();

        _context.SLAConfigurations.Add(sla);
        await _context.SaveChangesAsync(default);

        return Ok(sla);
    }
}
