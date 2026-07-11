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

    public UsersController(IMediator mediator, IOmniFlowDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { UserId = id });
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAll()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();
        return Ok(user);
    }
}
