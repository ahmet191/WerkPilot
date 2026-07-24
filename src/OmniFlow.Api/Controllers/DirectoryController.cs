using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// One row of the employee directory. A named shape rather than two anonymous
/// ones: the directory is stitched together from a profile query and a
/// profile-less user query, and anonymous types only unify when every member's
/// type *and* nullability match exactly — a trap that is easy to reintroduce.
/// </summary>
public sealed record EmployeeDirectoryEntry
{
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Title { get; init; }
    public string Department { get; init; } = "Unknown";
    public string? Bio { get; init; }
    public List<string> Skills { get; init; } = new();
    /// <summary>Work address, or the personal one when the profile opts in — either may be absent.</summary>
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateTime? DateOfBirth { get; init; }
    public string? ProfilePictureUrl { get; init; }
}

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
            .Select(dp => new EmployeeDirectoryEntry
            {
                UserId = dp.UserId,
                Name = dp.User.Name,
                Title = dp.User.Title,
                Department = dp.User.Department != null ? dp.User.Department.LocalizedName.GetValueOrDefault("en") : "Unknown",
                Bio = dp.Bio,
                Skills = dp.Skills,
                Email = dp.ShowPersonalEmail ? dp.User.PersonalEmail : dp.User.Email,
                Phone = dp.ShowPhoneNumber ? dp.User.PhoneNumber : null,
                DateOfBirth = dp.ShowDateOfBirth ? dp.User.DateOfBirth : null,
                ProfilePictureUrl = dp.User.ProfilePictureUrl
            })
            .ToListAsync();

        // Include users who don't have a specific directory profile yet but exist in the system
        var allUsers = await _context.Users
            .Include(u => u.Department)
            .Where(u => !profiles.Select(p => p.UserId).Contains(u.Id))
            .Select(u => new EmployeeDirectoryEntry
            {
                UserId = u.Id,
                Name = u.Name,
                Title = u.Title,
                Department = u.Department != null ? u.Department.LocalizedName.GetValueOrDefault("en") : "Unknown",
                Email = u.Email,
                ProfilePictureUrl = u.ProfilePictureUrl
            })
            .ToListAsync();

        return Ok(profiles.Concat(allUsers));
    }

    [HttpGet("orgchart")]
    public async Task<IActionResult> GetOrgChart()
    {
        var users = await _context.Users
            .Include(u => u.Department)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Title,
                u.Email,
                u.ManagerId,
                DepartmentName = u.Department != null ? u.Department.LocalizedName.GetValueOrDefault("en") : "Executive & Operations",
                u.ProfilePictureUrl,
                u.PhoneNumber
            })
            .ToListAsync();

        return Ok(users);
    }
}
