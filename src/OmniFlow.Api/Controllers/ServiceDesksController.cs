using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Get()
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

        var desks = await query.Select(sd => new
        {
            sd.Id,
            sd.Name,
            sd.Description,
            sd.IsPublicToAllEmployees
        }).ToListAsync();

        return Ok(desks);
    }
}
