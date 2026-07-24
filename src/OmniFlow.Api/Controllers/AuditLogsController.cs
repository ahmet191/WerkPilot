using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.AuditLogs.Queries;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PermissionPolicyNames.AuditLogsView)]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] OmniFlow.Application.Common.Models.GridRequest request)
    {
        var logs = await _mediator.Send(new GetAuditLogsQuery(request));
        return Ok(logs);
    }
}
