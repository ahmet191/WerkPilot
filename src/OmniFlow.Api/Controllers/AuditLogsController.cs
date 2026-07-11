using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.AuditLogs.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int current = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = null, [FromQuery] bool sortDesc = false, [FromQuery] string? entityType = null)
    {
        var pagedRequest = new OmniFlow.Application.Common.Models.PagedRequest
        {
            PageNumber = current,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDesc = sortDesc
        };

        if (!string.IsNullOrEmpty(entityType))
        {
            pagedRequest.Filters.Add("entityType", entityType);
        }

        var logs = await _mediator.Send(new GetAuditLogsQuery(pagedRequest));
        return Ok(logs);
    }
}
