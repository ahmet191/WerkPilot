using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Entities;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// The signed-in user's notification centre. Every query is scoped to the
/// caller — a notification is only ever readable by its recipient.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;

    public NotificationsController(IOmniFlowDbContext context)
    {
        _context = context;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new ForbiddenAccessException("The access token carries no subject.");

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 20, [FromQuery] bool unreadOnly = false)
    {
        var userId = CurrentUserId;
        var query = _context.Notifications.Where(n => n.RecipientUserId == userId);
        if (unreadOnly)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .Select(n => new
            {
                id = n.Id,
                kind = n.Kind.ToString(),
                title = n.Title,
                body = n.Body,
                link = n.Link,
                createdAt = n.CreatedAt,
                isRead = n.ReadAt != null
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(items);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = CurrentUserId;
        var count = await _context.Notifications
            .CountAsync(n => n.RecipientUserId == userId && n.ReadAt == null, HttpContext.RequestAborted);
        return Ok(new { count });
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = CurrentUserId;
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId, HttpContext.RequestAborted)
            ?? throw new NotFoundException(nameof(Notification), id);

        notification.MarkRead();
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = CurrentUserId;
        var unread = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && n.ReadAt == null)
            .ToListAsync(HttpContext.RequestAborted);

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }
        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        return Ok(new { marked = unread.Count });
    }
}
