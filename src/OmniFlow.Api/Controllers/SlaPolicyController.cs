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
    /// Serves Advanced SLA Policy Engine & Escalation Matrix management.
    /// </summary>
    [ApiController]
    [Route("api/slas")]
    [Authorize]
    public class SlaPolicyController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SlaPolicyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentTenantId =>
            Guid.TryParse(User.FindFirstValue("tenant_id"), out var id)
                ? id
                : Guid.Parse("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// Gets SLA policy rules and escalation settings for the tenant.
        /// </summary>
        [HttpGet("policies")]
        public async Task<ActionResult<List<SlaPolicyRuleDto>>> GetPolicies(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetSlaPolicyRulesQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Saves an SLA policy rule and escalation matrix definition.
        /// </summary>
        [HttpPost("policies")]
        public async Task<ActionResult<Guid>> SavePolicy([FromBody] SaveSlaPolicyRuleCommand command, CancellationToken cancellationToken)
        {
            command.TenantId = CurrentTenantId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
