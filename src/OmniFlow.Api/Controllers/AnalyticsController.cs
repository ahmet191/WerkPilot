using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;

    public AnalyticsController(IOmniFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var totalAssets = await _context.Assets.CountAsync();
        var totalTickets = await _context.Tickets.CountAsync();

        var openTickets = await _context.Tickets
            .Where(t => t.CurrentState == TicketState.New || t.CurrentState == TicketState.InProgress || t.CurrentState == TicketState.WaitingForApproval)
            .CountAsync();

        var breachedSla = await _context.Tickets
            .Where(t => t.SlaDueDate.HasValue && t.SlaDueDate.Value < DateTimeOffset.UtcNow && t.CurrentState != TicketState.Resolved && t.CurrentState != TicketState.Closed)
            .CountAsync();

        return Ok(new
        {
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            BreachedSla = breachedSla,
            TotalAssets = totalAssets
        });
    }

    /// <summary>Ticket counts grouped by workflow state.</summary>
    [HttpGet("reports/tickets-by-status")]
    public async Task<IActionResult> TicketsByStatus()
    {
        var rows = await _context.Tickets
            .GroupBy(t => t.CurrentState)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        return Ok(rows.OrderByDescending(r => r.Count));
    }

    /// <summary>Ticket counts grouped by priority.</summary>
    [HttpGet("reports/tickets-by-priority")]
    public async Task<IActionResult> TicketsByPriority()
    {
        var rows = await _context.Tickets
            .GroupBy(t => t.Priority)
            .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();
        // Present in severity order rather than count order.
        var order = new[] { "Critical", "High", "Medium", "Low" };
        return Ok(rows.OrderBy(r => Array.IndexOf(order, r.Priority)));
    }

    /// <summary>Tickets created per day over the last <paramref name="days"/> days.</summary>
    [HttpGet("reports/volume-trend")]
    public async Task<IActionResult> VolumeTrend([FromQuery] int days = 30)
    {
        days = Math.Clamp(days, 7, 180);
        var startDate = DateTime.UtcNow.Date.AddDays(-days + 1);
        var since = new DateTimeOffset(startDate, TimeSpan.Zero);

        // Group by day in memory: the window is bounded and this sidesteps
        // provider-specific date-truncation translation.
        var created = await _context.Tickets
            .Where(t => t.CreatedAt >= since)
            .Select(t => t.CreatedAt)
            .ToListAsync();

        var byDay = created.GroupBy(d => d.UtcDateTime.Date).ToDictionary(g => g.Key, g => g.Count());
        var series = Enumerable.Range(0, days)
            .Select(i => startDate.AddDays(i))
            .Select(d => new { Date = d.ToString("yyyy-MM-dd"), Count = byDay.GetValueOrDefault(d, 0) });
        return Ok(series);
    }

    /// <summary>SLA compliance: how many tickets with an SLA are within it vs breached.</summary>
    [HttpGet("reports/sla-compliance")]
    public async Task<IActionResult> SlaCompliance()
    {
        var now = DateTimeOffset.UtcNow;
        var withSla = _context.Tickets.Where(t => t.SlaDueDate.HasValue);

        var total = await withSla.CountAsync();
        // Breached = resolved after the deadline, or still open past the deadline.
        // The open-state check is inlined because EF can't translate a custom method.
        var breached = await withSla.CountAsync(t =>
            (t.ResolutionDate.HasValue && t.ResolutionDate > t.SlaDueDate) ||
            (!t.ResolutionDate.HasValue && t.SlaDueDate < now &&
             (t.CurrentState == TicketState.New || t.CurrentState == TicketState.InProgress || t.CurrentState == TicketState.WaitingForApproval)));

        var compliant = total - breached;
        var pct = total == 0 ? 100 : Math.Round(compliant * 100.0 / total, 1);
        return Ok(new { Total = total, Compliant = compliant, Breached = breached, CompliancePct = pct });
    }

    /// <summary>Per-agent workload: how many tickets each assignee owns and has resolved.</summary>
    [HttpGet("reports/team-performance")]
    public async Task<IActionResult> TeamPerformance()
    {
        var rows = await _context.Tickets
            .Where(t => t.AssigneeId != null)
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g => new
            {
                AssigneeId = g.Key,
                Assigned = g.Count(),
                Open = g.Count(t => t.CurrentState == TicketState.New || t.CurrentState == TicketState.InProgress || t.CurrentState == TicketState.WaitingForApproval),
                Resolved = g.Count(t => t.CurrentState == TicketState.Resolved || t.CurrentState == TicketState.Closed),
            })
            .ToListAsync();

        var names = await _context.Users
            .Where(u => rows.Select(r => r.AssigneeId).Contains(u.Id))
            .Select(u => new { u.Id, u.Name })
            .ToDictionaryAsync(u => u.Id, u => u.Name);

        return Ok(rows
            .Select(r => new { Agent = names.GetValueOrDefault(r.AssigneeId, "Unknown"), r.Assigned, r.Open, r.Resolved })
            .OrderByDescending(r => r.Assigned));
    }

    /// <summary>Exports the ticket list as CSV (respects the tenant filter).</summary>
    [HttpGet("reports/export/tickets.csv")]
    public async Task<IActionResult> ExportTicketsCsv()
    {
        var tickets = await _context.Tickets
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Title,
                Status = t.CurrentState.ToString(),
                Priority = t.Priority.ToString(),
                Type = t.Type.ToString(),
                Created = t.CreatedAt,
                SlaDue = t.SlaDueDate,
                Resolved = t.ResolutionDate,
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Status,Priority,Type,Created,SlaDue,Resolved");
        foreach (var t in tickets)
        {
            sb.AppendLine(string.Join(',',
                Csv(t.Id.ToString()),
                Csv(t.Title),
                Csv(t.Status),
                Csv(t.Priority),
                Csv(t.Type),
                Csv(t.Created.ToString("u", CultureInfo.InvariantCulture)),
                Csv(t.SlaDue?.ToString("u", CultureInfo.InvariantCulture) ?? ""),
                Csv(t.Resolved?.ToString("u", CultureInfo.InvariantCulture) ?? "")));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "tickets.csv");
    }

    // Minimal RFC-4180 quoting: wrap in quotes and double any embedded quotes
    // when the value contains a comma, quote or newline.
    private static string Csv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
