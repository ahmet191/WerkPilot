using MediatR;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Departments.Commands;
using OmniFlow.Application.Departments.Queries;

namespace OmniFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] OmniFlow.Application.Common.Models.GridRequest request)
    {
        var result = await _mediator.Send(new GetDepartmentsQuery(request));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(Get), new { id }, id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("Id mismatch");
        }
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteDepartmentCommand { Id = id });
        return NoContent();
    }
}
