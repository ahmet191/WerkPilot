using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.LeaveRequests.Commands;
using OmniFlow.Application.LeaveRequests.Queries;

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
    public async Task<IActionResult> GetMyLeaves([FromQuery] int current = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false)
    {
        var pagedRequest = new OmniFlow.Application.Common.Models.PagedRequest
        {
            PageNumber = current,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDesc = sortDesc
        };
        var leaves = await _mediator.Send(new GetMyLeaveRequestsQuery(pagedRequest));
        return Ok(leaves);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { Id = id });
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Policy = "HRAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveLeaveRequestCommand command)
    {
        command.Id = id;
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
