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

    public TemplatesController(IOmniFlowDbContext context)
    {
        _context = context;
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
    public async Task<ActionResult<List<TicketTemplate>>> GetAll()
    {
        var templates = await _context.TicketTemplates.ToListAsync();
        return Ok(templates);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketTemplate>> GetById(Guid id)
    {
        var template = await _context.TicketTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        return Ok(template);
    }
}
