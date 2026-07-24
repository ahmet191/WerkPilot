using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Authorization;

namespace OmniFlow.Api.Controllers;

/// <summary>
/// Files attached to a ticket. Bytes stream through <see cref="IFileStorage"/>;
/// only metadata is persisted. Everything is tenant-scoped through the global
/// query filter — a download can only ever reach the caller's own tenant.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketAttachmentsController : ControllerBase
{
    // Defence in depth against malicious uploads: an allowlist of the content
    // types a service desk actually needs, plus a hard size cap.
    private const long MaxBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "application/pdf", "text/plain", "text/csv",
        "application/zip", "application/x-zip-compressed",
        "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/octet-stream",
    };

    private readonly IOmniFlowDbContext _context;
    private readonly IFileStorage _storage;
    private readonly IUserContext _userContext;
    private readonly INotificationService _notifications;

    public TicketAttachmentsController(
        IOmniFlowDbContext context,
        IFileStorage storage,
        IUserContext userContext,
        INotificationService notifications)
    {
        _context = context;
        _storage = storage;
        _userContext = userContext;
        _notifications = notifications;
    }

    [HttpGet("{ticketId:guid}/attachments")]
    [Authorize(Policy = PermissionPolicyNames.TicketsView)]
    public async Task<IActionResult> List(Guid ticketId)
    {
        var items = await _context.TicketAttachments
            .Where(a => a.TicketId == ticketId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                id = a.Id,
                fileName = a.FileName,
                contentType = a.ContentType,
                sizeBytes = a.SizeBytes,
                uploadedById = a.UploadedById,
                uploadedByName = a.UploadedBy != null ? a.UploadedBy.Name : string.Empty,
                createdAt = a.CreatedAt
            })
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(items);
    }

    [HttpPost("{ticketId:guid}/attachments")]
    [Authorize(Policy = PermissionPolicyNames.TicketsEdit)]
    [RequestSizeLimit(MaxBytes + 1024)]
    public async Task<IActionResult> Upload(Guid ticketId, IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException("file", "No file was uploaded.");
        }
        if (file.Length > MaxBytes)
        {
            throw new ValidationException("file", $"The file exceeds the {MaxBytes / (1024 * 1024)} MB limit.");
        }
        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            throw new ValidationException("file", $"Files of type '{file.ContentType}' are not allowed.");
        }

        var ticket = await _context.Tickets
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == ticketId, HttpContext.RequestAborted)
            ?? throw new NotFoundException(nameof(Ticket), ticketId);

        await using var stream = file.OpenReadStream();
        var storageKey = await _storage.SaveAsync(ticket.TenantId, file.FileName, stream, HttpContext.RequestAborted);

        var attachment = ticket.AddAttachment(
            _userContext.UserId, SanitizeFileName(file.FileName), file.ContentType, file.Length, storageKey);
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        // Let the assignee know a file landed on their ticket.
        if (ticket.AssigneeId is { } assigneeId && assigneeId != _userContext.UserId)
        {
            await _notifications.NotifyUserAsync(
                assigneeId, NotificationKind.TicketStateChanged,
                "New attachment", $"A file was added to ticket '{ticket.Title}'.",
                $"/tickets/{ticket.Id}", HttpContext.RequestAborted);
        }

        return Ok(new { attachmentId = attachment.Id });
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    [Authorize(Policy = PermissionPolicyNames.TicketsView)]
    public async Task<IActionResult> Download(Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, HttpContext.RequestAborted)
            ?? throw new NotFoundException(nameof(TicketAttachment), attachmentId);

        var stream = await _storage.OpenReadAsync(attachment.StorageKey, HttpContext.RequestAborted);
        if (stream is null)
        {
            throw new NotFoundException(nameof(TicketAttachment), attachmentId);
        }

        return File(stream, attachment.ContentType, attachment.FileName);
    }

    [HttpDelete("attachments/{attachmentId:guid}")]
    [Authorize(Policy = PermissionPolicyNames.TicketsEdit)]
    public async Task<IActionResult> Delete(Guid attachmentId)
    {
        var attachment = await _context.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId, HttpContext.RequestAborted)
            ?? throw new NotFoundException(nameof(TicketAttachment), attachmentId);

        var storageKey = attachment.StorageKey;
        _context.TicketAttachments.Remove(attachment); // soft delete via the interceptor
        await _context.SaveChangesAsync(HttpContext.RequestAborted);

        // The row is soft-deleted but the bytes are gone for good — attachments
        // are not something we restore.
        await _storage.DeleteAsync(storageKey, HttpContext.RequestAborted);

        return NoContent();
    }

    /// <summary>Keeps the display name safe; the stored path never uses it.</summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Length > 260 ? name[^260..] : name;
    }
}
