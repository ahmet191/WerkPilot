using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Users.Commands;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public UsersController(IMediator mediator, IOmniFlowDbContext context, IUserContext userContext)
    {
        _mediator = mediator;
        _context = context;
        _userContext = userContext;
    }

    [HttpPost]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    public async Task<IActionResult> Create(CreateUserCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { UserId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] GridRequest request)
    {
        var lang = _userContext.Language;

        // Localized department names come from a JSONB dictionary, so the
        // projection happens in memory; grid search/filter/sort/paging is then
        // applied server-side over the projected shape.
        var users = await _context.Users
            .Include(u => u.Department)
            .ToListAsync();

        var result = users.Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                Role = u.Role.ToString(),
                u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.LocalizedName.Translations.ContainsKey(lang) ? u.Department.LocalizedName.Translations[lang] : (u.Department.LocalizedName.Translations.ContainsKey("en") ? u.Department.LocalizedName.Translations["en"] : "Unknown") : "None"
            })
            .ToGridResult(request, ["Name", "Email", "Role", "DepartmentName"]);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Id mismatch");
        }
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PermissionPolicyNames.UsersManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteUserCommand { Id = id });
        return NoContent();
    }
}
