using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Tenants.Commands;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Infrastructure.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers
{
    /// <summary>
    /// Serves Custom Department Studio management for dynamic tenant departments.
    /// </summary>
    [ApiController]
    [Route("api/departments")]
    [Authorize]
    public class CustomDepartmentController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomDepartmentController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentTenantId =>
            Guid.TryParse(User.FindFirstValue("tenant_id"), out var id)
                ? id
                : Guid.Parse("00000000-0000-0000-0000-000000000001"); // Fallback baseline tenant ID

        /// <summary>
        /// Gets custom departments configured for the current tenant.
        /// </summary>
        [HttpGet("studio")]
        public async Task<ActionResult<List<CustomDepartmentDto>>> GetCustomDepartments(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetCustomDepartmentsQuery(CurrentTenantId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Creates or updates a custom department in the Department Studio.
        /// </summary>
        [HttpPost("studio")]
        [Authorize(Policy = PermissionPolicyNames.UsersManage)]
        public async Task<ActionResult<Guid>> CreateOrUpdateDepartment([FromBody] CreateOrUpdateCustomDepartmentCommand command, CancellationToken cancellationToken)
        {
            command.TenantId = CurrentTenantId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>Deletes a custom department from the Department Studio.</summary>
        [HttpDelete("studio/{id:guid}")]
        [Authorize(Policy = PermissionPolicyNames.UsersManage)]
        public async Task<ActionResult> DeleteDepartment(Guid id, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteCustomDepartmentCommand(id, CurrentTenantId), cancellationToken);
            return NoContent();
        }
    }
}
