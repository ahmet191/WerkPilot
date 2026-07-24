using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceDesksController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public ServiceDesksController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GridRequest request)
    {
        // Simple logic for visibility:
        // Admin sees all. Others see Public ones + ones allowed for their department.

        var query = _context.ServiceDesks.AsQueryable();

        if (_userContext.Role != "Admin")
        {
            // In a real scenario, we would check User's DepartmentId against AllowedDepartmentIds
            // For now, we return Public ones.
            query = query.Where(sd => sd.IsPublicToAllEmployees);
        }

        // Fully SQL-translated: search/filter/sort/paging all run in Postgres.
        var desks = await query.Select(sd => new
        {
            sd.Id,
            sd.Name,
            sd.Description,
            sd.IsPublicToAllEmployees
        }).ToGridResultAsync(request, ["Name", "Description"]);

        return Ok(desks);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceDeskRequest request)
    {
        var desk = new ServiceDesk
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            DepartmentId = request.DepartmentId,
            IsPublicToAllEmployees = true // Default to true as per existing visibility rules
        };

        _context.ServiceDesks.Add(desk);
        await _context.SaveChangesAsync(default);

        return Ok(desk);
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceDeskRequest request)
    {
        var desk = await _context.ServiceDesks.FindAsync(id);
        if (desk == null) return NotFound();

        desk.Name = request.Name;
        desk.Description = request.Description ?? string.Empty;
        desk.DepartmentId = request.DepartmentId;
        // Defaulting to true or keeping it from request if it existed
        
        await _context.SaveChangesAsync(default);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var desk = await _context.ServiceDesks.FindAsync(id);
        if (desk == null) return NotFound();

        _context.ServiceDesks.Remove(desk);
        await _context.SaveChangesAsync(default);
        return NoContent();
    }
}

public class CreateServiceDeskRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
}

public class UpdateServiceDeskRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid DepartmentId { get; set; }
}
