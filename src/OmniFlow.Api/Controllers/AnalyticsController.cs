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
}
