using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Get()
    {
        // For security, users should only see their own expenses unless they are an admin or manager.
        var query = _context.ExpenseClaims.Include(e => e.User).AsQueryable();

        if (_userContext.Role != "Admin")
        {
            query = query.Where(e => e.UserId == _userContext.UserId);
        }

        var expenses = await query
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        return Ok(expenses);
    }
}
