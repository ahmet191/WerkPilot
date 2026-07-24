using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Tickets.Commands;

namespace OmniFlow.Api.Controllers
{
    /// <summary>
    /// Serves Dynamic Ticket Form Designer management per Service Desk.
    /// </summary>
    [ApiController]
    [Route("api/forms")]
    [Authorize]
    public class FormDesignerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FormDesignerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentTenantId =>
            Guid.TryParse(User.FindFirstValue("tenant_id"), out var id)
                ? id
                : Guid.Parse("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Gets dynamic form schemas configured for the tenant's Service Desks.
        /// </summary>
        [HttpGet("schemas")]
        public async Task<ActionResult<List<TicketFormSchemaDto>>> GetSchemas(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetTicketFormSchemasQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Saves a dynamic form schema for a Service Desk.
        /// </summary>
        [HttpPost("schemas")]
        public async Task<ActionResult<Guid>> SaveSchema([FromBody] SaveTicketFormSchemaCommand command, CancellationToken cancellationToken)
        {
            command.TenantId = CurrentTenantId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
