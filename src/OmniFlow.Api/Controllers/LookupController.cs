using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/lookup")]
public class LookupController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;

    public LookupController(IOmniFlowDbContext context)
    {
        _context = context;
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
        var departments = await _context.Departments
            .Select(d => new { label = d.LocalizedName.Translations.FirstOrDefault().Value ?? "Unknown", value = d.Id })
            .ToListAsync();
        return Ok(departments);
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
