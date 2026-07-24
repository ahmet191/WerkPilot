using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Tenants.Queries;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Infrastructure.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers
{
    /// <summary>
    /// Serves module and service desk visibility controls per tenant and role.
    /// Allows Admins to dynamically control which service desks (IT, HR, Finance, Legal, Facilities)
    /// are visible to which roles.
    /// </summary>
    [ApiController]
    [Route("api/visibility")]
    [Authorize]
    public class DepartmentVisibilityController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DepartmentVisibilityController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new ForbiddenAccessException("The access token carries no subject.");

        private string CurrentRole =>
            User.FindFirstValue(ClaimTypes.Role) ?? "Requester";

        /// <summary>
        /// Gets list of department service desks visible to current user based on visibility matrix.
        /// </summary>
        [HttpGet("my-desks")]
        public async Task<ActionResult<List<ServiceDeskCardDto>>> GetMyVisibleDesks(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyVisibleDesksQuery(CurrentUserId, CurrentRole), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin endpoint to fetch tenant visibility matrix.
        /// </summary>
        [HttpGet("matrix")]
        [Authorize(Policy = PermissionPolicyNames.PermissionsManage)]
        public async Task<ActionResult<List<VisibilityMatrixItemDto>>> GetVisibilityMatrix(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetVisibilityMatrixQuery(), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Admin endpoint to update visibility rule for a role and department key.
        /// </summary>
        [HttpPut("matrix")]
        [Authorize(Policy = PermissionPolicyNames.PermissionsManage)]
        public async Task<ActionResult<bool>> UpdateVisibilityRule([FromBody] UpdateVisibilityMatrixCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
