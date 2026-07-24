using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Dashboard.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics()
    {
        var result = await _mediator.Send(new GetMetricsQuery());
        return Ok(result);
    }
}
