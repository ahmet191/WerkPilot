using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public TemplatesController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpPost]
    public async Task<IActionResult> Create(TicketTemplate template)
    {
        if (template.Id == Guid.Empty)
        {
            template.Id = Guid.NewGuid();
        }

        _context.TicketTemplates.Add(template);
        await _context.SaveChangesAsync(default);

        return Ok(new { TemplateId = template.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var templates = await _context.TicketTemplates
            .Include(t => t.ServiceDesk)
            .Select(t => new
            {
                t.Id,
                t.ServiceDeskId,
                ServiceDeskName = t.ServiceDesk != null ? t.ServiceDesk.Name : "Unknown",
                Fields = t.Fields,
                Workflow = t.Workflow,
                LocalizedName = t.LocalizedName
            })
            .ToListAsync();
        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketTemplate>> GetById(Guid id)
    {
        var template = await _context.TicketTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] TicketTemplate template)
    {
        var existing = await _context.TicketTemplates.FindAsync(id);
        if (existing == null) return NotFound();

        existing.LocalizedName = template.LocalizedName;
        existing.ServiceDeskId = template.ServiceDeskId;
        existing.Fields = template.Fields;
        existing.Workflow = template.Workflow;

        await _context.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _context.TicketTemplates.FindAsync(id);
        if (existing == null) return NotFound();

        _context.TicketTemplates.Remove(existing);
        await _context.SaveChangesAsync(default);
        return NoContent();
    }
}
