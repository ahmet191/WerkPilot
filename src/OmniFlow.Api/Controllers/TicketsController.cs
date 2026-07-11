using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Application.Tickets.Commands;
using OmniFlow.Application.Tickets.Queries;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/tickets")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOmniFlowDbContext _context;

    public TicketsController(IMediator mediator, IOmniFlowDbContext context)
    {
        _mediator = mediator;
        _context = context;
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
    public async Task<ActionResult<Ticket>> GetDetails(Guid id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Template)
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.AuditLogs).ThenInclude(a => a.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();
        return Ok(ticket);
    }

    [HttpGet("requester/{requesterId}")]
    public async Task<ActionResult<List<Ticket>>> GetByRequester(Guid requesterId)
    {
        var query = new GetTicketsQuery(requesterId);
        var tickets = await _mediator.Send(query);
        return Ok(tickets);
    }
}

public record TransitionRequest(string Trigger, Guid UserId);
public record AssignRequest(Guid? AssigneeId, Guid UserId);
public record CommentRequest(Guid AuthorId, string Content);
