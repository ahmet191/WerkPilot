using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
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
    public async Task<IActionResult> Get([FromQuery] GridRequest request)
    {
        var lang = _userContext.Language;
        var slas = await _context.SLAConfigurations
            .Include(s => s.Department)
            .ToListAsync();

        // Localized JSONB projection — grid ops applied server-side in memory.
        var result = slas.Select(s => new
            {
                s.Id,
                s.Name,
                s.PriorityLevel,
                s.ResolutionTimeMinutes,
                s.FirstResponseTimeMinutes,
                s.DepartmentId,
                DepartmentName = s.Department != null ? s.Department.LocalizedName.Translations.ContainsKey(lang) ? s.Department.LocalizedName.Translations[lang] : (s.Department.LocalizedName.Translations.ContainsKey("en") ? s.Department.LocalizedName.Translations["en"] : "Unknown") : "None"
            })
            .ToGridResult(request, ["Name", "DepartmentName"]);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Create(SLAConfiguration sla)
    {
        if (sla.Id == Guid.Empty)
            sla.Id = Guid.NewGuid();

        _context.SLAConfigurations.Add(sla);
        await _context.SaveChangesAsync(default);

        return Ok(sla);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SLAConfiguration sla)
    {
        var existing = await _context.SLAConfigurations.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = sla.Name;
        existing.PriorityLevel = sla.PriorityLevel;
        existing.ResolutionTimeMinutes = sla.ResolutionTimeMinutes;
        existing.FirstResponseTimeMinutes = sla.FirstResponseTimeMinutes;
        existing.DepartmentId = sla.DepartmentId;

        await _context.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "Administrator")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _context.SLAConfigurations.FindAsync(id);
        if (existing == null) return NotFound();

        _context.SLAConfigurations.Remove(existing);
        await _context.SaveChangesAsync(default);
        return NoContent();
    }
}
