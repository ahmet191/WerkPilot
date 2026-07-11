using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DirectoryController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;

    public DirectoryController(IOmniFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet("employees")]
    public async Task<IActionResult> GetEmployees()
    {
        // Safe employee directory DTO for public viewing
        var profiles = await _context.DirectoryProfiles
            .Include(dp => dp.User)
            .ThenInclude(u => u.Department)
            .Select(dp => new
            {
                dp.UserId,
                Name = dp.User.Name,
                Title = dp.User.Title,
                Department = dp.User.Department != null ? dp.User.Department.LocalizedName.GetValueOrDefault("en") : "Unknown",
                dp.Bio,
                dp.Skills,
                Email = dp.ShowPersonalEmail ? dp.User.PersonalEmail : dp.User.Email,
                Phone = dp.ShowPhoneNumber ? dp.User.PhoneNumber : null,
                DateOfBirth = dp.ShowDateOfBirth ? dp.User.DateOfBirth : null,
                dp.User.ProfilePictureUrl
            })
            .ToListAsync();

        // Include users who don't have a specific directory profile yet but exist in the system
        var allUsers = await _context.Users
            .Include(u => u.Department)
            .Where(u => !profiles.Select(p => p.UserId).Contains(u.Id))
            .Select(u => new
            {
                UserId = u.Id,
                Name = u.Name,
                Title = u.Title,
                Department = u.Department != null ? u.Department.LocalizedName.GetValueOrDefault("en") : "Unknown",
                Bio = (string?)null,
                Skills = new List<string>(),
                Email = u.Email,
                Phone = (string?)null,
                DateOfBirth = (DateTime?)null,
                u.ProfilePictureUrl
            })
            .ToListAsync();

        return Ok(profiles.Concat(allUsers));
    }

    [HttpGet("orgchart")]
    public async Task<IActionResult> GetOrgChart()
    {
        var users = await _context.Users
            .Select(u => new { u.Id, u.Name, u.Title, u.ManagerId })
            .ToListAsync();

        return Ok(users);
    }
}
