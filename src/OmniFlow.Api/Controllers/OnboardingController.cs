using System;
using System.Collections.Generic;
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
    /// <summary>
    /// API for Onboarding & Offboarding checklist workflow tracking across IT, HR, Facilities, and Finance.
    /// </summary>
    [ApiController]
    [Route("api/onboarding")]
    [Authorize]
    public class OnboardingController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OnboardingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new ForbiddenAccessException("The access token carries no subject.");

        /// <summary>
        /// Gets onboarding/offboarding tasks filtered by employee or department.
        /// </summary>
        [HttpGet("checklist")]
        public async Task<ActionResult<List<OnboardingTaskDto>>> GetChecklist(
            [FromQuery] Guid? employeeUserId,
            [FromQuery] string? department,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetOnboardingChecklistQuery(employeeUserId, department), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Completes or toggles completion status for an onboarding/offboarding task.
        /// </summary>
        [HttpPost("tasks/{taskId:guid}/complete")]
        public async Task<ActionResult<bool>> CompleteTask(
            Guid taskId,
            [FromBody] CompleteOnboardingTaskCommand command,
            CancellationToken cancellationToken)
        {
            command.TaskId = taskId;
            command.CompletedByUserId = CurrentUserId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
