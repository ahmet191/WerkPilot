using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Approvals.Commands;
using OmniFlow.Application.Approvals.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/approvals")]
public class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Approval steps currently waiting on the calling user.</summary>
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var items = await _mediator.Send(new GetMyPendingApprovalsQuery());
        return Ok(items);
    }

    /// <summary>Approval history/timeline of a governed entity.</summary>
    [HttpGet("entity/{entityType}/{entityId:guid}")]
    public async Task<IActionResult> GetForEntity(string entityType, Guid entityId)
    {
        var items = await _mediator.Send(new GetApprovalsForEntityQuery(entityType, entityId));
        return Ok(items);
    }

    /// <summary>Approve or reject the current step of a pending request.</summary>
    [HttpPost("{id:guid}/decide")]
    public async Task<IActionResult> Decide(Guid id, [FromBody] DecideRequest request)
    {
        var result = await _mediator.Send(new DecideApprovalStepCommand(id, request.Approve, request.Comment));
        return Ok(new { Success = result });
    }
}

public record DecideRequest(bool Approve, string? Comment);
