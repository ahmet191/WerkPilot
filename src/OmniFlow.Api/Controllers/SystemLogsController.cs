using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Admin.Queries;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/admin/systemlogs")]
[Authorize(Policy = PermissionPolicyNames.SystemLogsView)]
public class SystemLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SystemLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] GetSystemLogsQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(new
        {
            items = result.Items,
            totalCount = result.TotalCount
        });
    }

    [HttpGet("trace/{traceId}")]
    public async Task<IActionResult> GetTraceDetails(string traceId)
    {
        var items = await _mediator.Send(new GetTraceDetailsQuery(traceId));
        return Ok(items);
    }
}
