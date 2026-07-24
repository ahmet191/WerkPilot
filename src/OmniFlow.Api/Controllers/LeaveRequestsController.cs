using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.LeaveRequests.Commands;
using OmniFlow.Application.LeaveRequests.Queries;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyLeaves([FromQuery] OmniFlow.Application.Common.Models.GridRequest request)
    {
        var leaves = await _mediator.Send(new GetMyLeaveRequestsQuery(request));
        return Ok(leaves);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { Id = id });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = PermissionPolicyNames.LeavesApprove)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLeaveRequestCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
