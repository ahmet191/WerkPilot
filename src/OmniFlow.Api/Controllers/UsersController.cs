using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Users.Commands;
using OmniFlow.Domain.Entities;

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
    public async Task<IActionResult> Create(CreateUserCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { UserId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lang = _userContext.Language;
        var users = await _context.Users
            .Include(u => u.Department)
            .ToListAsync();

        var result = users.Select(u => new
            {
                u.Id,
                u.Name,
                u.Email,
                u.Role,
                u.DepartmentId,
                DepartmentName = u.Department != null ? u.Department.LocalizedName.Translations.ContainsKey(lang) ? u.Department.LocalizedName.Translations[lang] : (u.Department.LocalizedName.Translations.ContainsKey("en") ? u.Department.LocalizedName.Translations["en"] : "Unknown") : "None"
            })
            .ToList();
            
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
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteUserCommand { Id = id });
        return NoContent();
    }
}
