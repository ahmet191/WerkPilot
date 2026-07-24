using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Menus.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuController : ControllerBase
{
    private readonly IMediator _mediator;

    public MenuController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetMenu()
    {
        var menu = await _mediator.Send(new GetMenuQuery());
        return Ok(menu);
    }
}
