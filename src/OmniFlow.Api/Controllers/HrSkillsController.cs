using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Employees.Commands;
using OmniFlow.Domain.Common.Exceptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers
{
    [ApiController]
    [Route("api/hr")]
    [Authorize]
    public class HrSkillsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HrSkillsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new ForbiddenAccessException("The access token carries no subject.");

        [HttpPost("skills")]
        public async Task<ActionResult<Guid>> AddSkill([FromBody] AddEmployeeSkillCommand command, CancellationToken cancellationToken)
        {
            command.EmployeeUserId = CurrentUserId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("certificates")]
        public async Task<ActionResult<Guid>> AddCertificate([FromBody] AddEmployeeCertificateCommand command, CancellationToken cancellationToken)
        {
            command.EmployeeUserId = CurrentUserId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
