using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.KnowledgeBase.Commands;
using OmniFlow.Application.KnowledgeBase.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KnowledgeBaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeBaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool onlyPublished = true)
    {
        var result = await _mediator.Send(new GetArticlesQuery { OnlyPublished = onlyPublished });
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArticleCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }
}
