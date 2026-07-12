using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Tickets.Commands;
using OmniFlow.Application.Tickets.Queries;
using OmniFlow.Domain.Entities;

using Microsoft.AspNetCore.Authorization;

namespace OmniFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public TicketsController(IMediator mediator, IOmniFlowDbContext context, IUserContext userContext)
    {
        _mediator = mediator;
        _context = context;
        _userContext = userContext;
    }

    /// <summary>
    /// Role-scoped ticket list feeding the grid. Privileged roles see every
    /// ticket in the tenant; requesters only their own. Search/filter/sort/
    /// paging all run in SQL — this is a high-volume table.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList([FromQuery] GridRequest request)
    {
        var query = _context.Tickets.AsNoTracking();

        var isPrivileged = _userContext.Role is "Administrator" or "Admin" or "ITAgent" or "HRAdmin" or "Agent";
        if (!isPrivileged)
        {
            var userId = _userContext.UserId;
            query = query.Where(t => t.RequesterId == userId);
        }

        if (string.IsNullOrEmpty(request.SortBy))
        {
            request.SortBy = "CreatedAt";
            request.SortDesc = true;
        }

        var result = await query
            .Select(t => new
            {
                t.Id,
                t.Title,
                Type = t.Type.ToString(),
                Priority = t.Priority.ToString(),
                Impact = t.Impact.ToString(),
                Urgency = t.Urgency.ToString(),
                CurrentState = t.CurrentState.ToString(),
                t.RequesterId,
                RequesterName = t.Requester != null ? t.Requester.Name : string.Empty,
                AssigneeName = t.Assignee != null ? t.Assignee.Name : string.Empty,
                t.SlaDueDate,
                t.CreatedAt
            })
            .ToGridResultAsync(request, ["Title", "RequesterName", "AssigneeName"]);

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTicketCommand command)
    {
        var ticketId = await _mediator.Send(command);
        return Ok(new { TicketId = ticketId });
    }

    [HttpPost("{id}/transition")]
    public async Task<IActionResult> Transition(Guid id, [FromBody] TransitionRequest request)
    {
        var command = new TransitionTicketCommand(id, request.Trigger, request.UserId);
        var result = await _mediator.Send(command);
        return Ok(new { Success = result });
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> Assign(Guid id, [FromBody] AssignRequest request)
    {
        var command = new AssignTicketCommand(id, request.AssigneeId, request.UserId);
        var result = await _mediator.Send(command);
        return Ok(new { Success = result });
    }

    [HttpPost("{id}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CommentRequest request)
    {
        var command = new AddTicketCommentCommand(id, request.AuthorId, request.Content);
        var commentId = await _mediator.Send(command);
        return Ok(new { CommentId = commentId });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDetailsDto>> GetDetails(Guid id)
    {
        var details = await _mediator.Send(new GetTicketDetailsQuery(id));
        return Ok(details);
    }

    [HttpGet("requester/{requesterId}")]
    public async Task<ActionResult<List<TicketDto>>> GetByRequester(Guid requesterId)
    {
        var tickets = await _mediator.Send(new GetTicketsQuery(requesterId));
        return Ok(tickets);
    }

    [HttpGet("servicedesk/{serviceDeskId}")]
    public async Task<ActionResult<List<TicketDto>>> GetByServiceDesk(Guid serviceDeskId)
    {
        var tickets = await _mediator.Send(new GetTicketsByServiceDeskQuery(serviceDeskId));
        return Ok(tickets);
    }
}

public record TransitionRequest(string Trigger, Guid UserId);
public record AssignRequest(Guid? AssigneeId, Guid UserId);
public record CommentRequest(Guid AuthorId, string Content);
