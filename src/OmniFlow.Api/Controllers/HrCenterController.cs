using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OmniFlow.Application.Employees.Commands;
using OmniFlow.Application.Employees.Queries;
using OmniFlow.Domain.Common.Exceptions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OmniFlow.Api.Controllers
{
    /// <summary>
    /// Employee Self-Service (ESS) & HR Center API for Özlük Hakları, Personal Details,
    /// Digital Document Vault, Assets, and Country-specific Legal Info (DE 🇩🇪 / TR 🇹🇷).
    /// </summary>
    [ApiController]
    [Route("api/hr")]
    [Authorize]
    public class HrCenterController : ControllerBase
    {
        private readonly IMediator _mediator;

        public HrCenterController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(Claims.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                ? id
                : throw new ForbiddenAccessException("The access token carries no subject.");

        /// <summary>
        /// Retrieves the current logged in employee's complete HR Profile & Özlük Hakları summary.
        /// </summary>
        [HttpGet("my-profile")]
        public async Task<ActionResult<HrProfileDto>> GetMyHrProfile(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyHrProfileQuery(CurrentUserId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Updates the logged in employee's personal, emergency contact, and financial details.
        /// </summary>
        [HttpPut("my-profile")]
        public async Task<ActionResult<bool>> UpdateMyHrProfile([FromBody] UpdateMyHrProfileCommand command, CancellationToken cancellationToken)
        {
            command.UserId = CurrentUserId;
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Lists documents in current employee's digital Özlük Evrak Kasası.
        /// </summary>
        [HttpGet("my-documents")]
        public async Task<ActionResult<List<EmployeeDocumentDto>>> GetMyDocuments(CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetMyEmployeeDocumentsQuery(CurrentUserId), cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Uploads a new document to current employee's Özlük Evrak Kasası.
        /// </summary>
        [HttpPost("my-documents")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<Guid>> UploadDocument(
            [FromForm] string category,
            [FromForm] string? title,
            [FromForm] DateTime? expirationDate,
            IFormFile file,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            using var stream = file.OpenReadStream();
            var command = new UploadEmployeeDocumentCommand
            {
                UserId = CurrentUserId,
                Category = category,
                Title = title ?? file.FileName,
                FileName = file.FileName,
                Content = stream,
                ContentType = file.ContentType,
                ExpirationDate = expirationDate
            };

            var docId = await _mediator.Send(command, cancellationToken);
            return Ok(docId);
        }
    }
}
