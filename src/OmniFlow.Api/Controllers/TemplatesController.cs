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

    /// <summary>
    /// Just the template's workflow, with the palette of states and roles the
    /// visual designer needs so it never hardcodes the domain enums.
    /// </summary>
    [HttpGet("{id}/workflow")]
    [Authorize(Policy = PermissionPolicyNames.TemplatesManage)]
    public async Task<IActionResult> GetWorkflow(Guid id)
    {
        var template = await _context.TicketTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();

        return Ok(new
        {
            templateId = template.Id,
            name = template.LocalizedName.GetValueOrDefault(_userContext.Language),
            workflow = template.Workflow,
            states = Enum.GetNames<Domain.Enums.TicketState>(),
            roles = Enum.GetNames<Domain.Enums.UserRole>(),
            // Identity roles that approval steps can address (distinct from the
            // domain UserRole used for a transition's RequiredRole).
            approverRoles = new[] { "Administrator", "HRAdmin", "ITAgent", "Requester" },
        });
    }

    /// <summary>
    /// Replaces the template's workflow only (Fields untouched). The workflow is
    /// validated first: a broken workflow would break every ticket on this
    /// template, so an invalid one is rejected with the specific errors.
    /// </summary>
    [HttpPut("{id}/workflow")]
    [Authorize(Policy = PermissionPolicyNames.TemplatesManage)]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] WorkflowDefinition workflow)
    {
        var template = await _context.TicketTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();

        var errors = OmniFlow.Application.Tickets.Workflow.WorkflowValidator.Validate(workflow);
        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { ["workflow"] = errors.ToArray() })
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Title = "One or more validation errors occurred.",
                Detail = "One or more validation failures have occurred."
            };
            return BadRequest(problem);
        }

        // A fresh reference from the request body: DetectChanges sees the new
        // object vs the tracked snapshot and marks the JSONB column modified.
        // (Unlike the seeder, which mutates the existing reference in place and
        // therefore has to flag IsModified by hand.)
        template.Workflow = workflow;
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
