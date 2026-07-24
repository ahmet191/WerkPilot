using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Common.Grid;
using OmniFlow.Application.Common.Models;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpenseClaimsController : ControllerBase
{
    private readonly IOmniFlowDbContext _context;
    private readonly IUserContext _userContext;

    public ExpenseClaimsController(IOmniFlowDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GridRequest request)
    {
        // For security, users should only see their own expenses unless they are an admin or manager.
        var query = _context.ExpenseClaims.AsQueryable();

        if (_userContext.Role != "Admin")
        {
            query = query.Where(e => e.UserId == _userContext.UserId);
        }

        // Default ordering preserved when the grid doesn't request a sort.
        if (string.IsNullOrEmpty(request.SortBy))
        {
            request.SortBy = nameof(ExpenseClaim.ExpenseDate);
            request.SortDesc = true;
        }

        // Fully SQL-translated grid over a slim projection (no entity/nav serialization).
        var expenses = await query
            .Select(e => new
            {
                e.Id,
                e.Title,
                e.Category,
                e.Amount,
                e.Currency,
                e.ExpenseDate,
                e.Status,
                e.UserId,
                UserName = e.User != null ? e.User.Name : string.Empty
            })
            .ToGridResultAsync(request, ["Title", "UserName"]);

        return Ok(expenses);
    }
}
