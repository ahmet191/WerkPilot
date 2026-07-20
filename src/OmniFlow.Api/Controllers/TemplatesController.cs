using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Authorization;

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
    [Authorize(Policy = PermissionPolicyNames.TemplatesManage)]
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
    public async Task<IActionResult> GetAll([FromQuery] GridRequest request)
    {
        var lang = _userContext.Language;

        var templates = await _context.TicketTemplates
            .Include(t => t.ServiceDesk)
            .ToListAsync();

        // Localized JSONB projection — grid ops applied server-side in memory.
        // Fields/Workflow/LocalizedName stay in the payload: the template picker
        // and the edit modal (FieldsBuilder/WorkflowBuilder) need them.
        var result = templates
            .Select(t => new
            {
                t.Id,
                Name = t.LocalizedName.GetValueOrDefault(lang),
                t.ServiceDeskId,
                ServiceDeskName = t.ServiceDesk != null ? t.ServiceDesk.Name : "Unknown",
                Fields = t.Fields,
                Workflow = t.Workflow,
                LocalizedName = t.LocalizedName
            })
            .ToGridResult(request, ["Name", "ServiceDeskName"]);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketTemplate>> GetById(Guid id)
    {
        var template = await _context.TicketTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        return Ok(template);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionPolicyNames.TemplatesManage)]
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
    [Authorize(Policy = PermissionPolicyNames.TemplatesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _context.TicketTemplates.FindAsync(id);
        if (existing == null) return NotFound();

        _context.TicketTemplates.Remove(existing);
        await _context.SaveChangesAsync(default);
        return NoContent();
    }
}
