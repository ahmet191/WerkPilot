using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Assets.Commands;
using OmniFlow.Application.Assets.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AssetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var result = await _mediator.Send(new GetAssetsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssetCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignAssetCommand command)
    {
        command.AssetId = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
