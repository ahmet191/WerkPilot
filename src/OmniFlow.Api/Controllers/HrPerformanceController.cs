using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Employees.Queries;
using OmniFlow.Domain.Common.Exceptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers
{
    /// <summary>
    /// Serves Phase 3 Performance OKRs, Skill Matrix, Certification Vault, and LMS Training management.
    /// </summary>
    [ApiController]
    [Route("api/hr")]
    [Authorize]
    public class HrPerformanceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HrPerformanceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new ForbiddenAccessException("The access token carries no subject.");

        /// <summary>
        /// Gets OKRs and performance goals for the logged-in employee.
        /// </summary>
        [HttpGet("goals")]
        public async Task<ActionResult<List<EmployeeGoalDto>>> GetGoals([FromQuery] int? year, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyGoalsQuery(CurrentUserId, year), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates or updates an employee performance goal/OKR.
        /// </summary>
        [HttpPost("goals")]
        public async Task<ActionResult<Guid>> CreateOrUpdateGoal([FromBody] CreateOrUpdateGoalCommand command, CancellationToken cancellationToken)
        {
            command.UserId = CurrentUserId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets skill matrix competencies and certification vault items.
        /// </summary>
        [HttpGet("skills-certificates")]
        public async Task<ActionResult<SkillsAndCertificatesBundleDto>> GetSkillsAndCertificates(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMySkillsAndCertificatesQuery(CurrentUserId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets assigned LMS training courses for the logged-in employee.
        /// </summary>
        [HttpGet("trainings")]
        public async Task<ActionResult<List<TrainingCourseDto>>> GetTrainings(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyTrainingsQuery(CurrentUserId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Completes an assigned LMS training course.
        /// </summary>
        [HttpPost("trainings/{courseId:guid}/complete")]
        public async Task<ActionResult<bool>> CompleteTraining(Guid courseId, [FromBody] CompleteTrainingCourseCommand command, CancellationToken cancellationToken)
        {
            command.CourseId = courseId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
