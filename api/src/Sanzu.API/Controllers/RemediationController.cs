using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/admin/remediation")]
public sealed class RemediationController : ControllerBase
{
    private readonly IRemediationService _remediationService;

    public RemediationController(IRemediationService remediationService)
    {
        _remediationService = remediationService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("preview")]
    [ProducesResponseType(typeof(ApiEnvelope<RemediationImpactPreviewResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(
        [FromQuery] string actionType,
        [FromQuery] Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _remediationService.PreviewAsync(actorUserId, actionType, tenantId, cancellationToken);
            return Ok(ApiEnvelope<RemediationImpactPreviewResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException ex)
        {
            return BadRequest(BuildValidationProblem(ex, "Invalid preview request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPost("commit")]
    [ProducesResponseType(typeof(ApiEnvelope<RemediationActionResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Commit(
        [FromBody] CommitRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _remediationService.CommitAsync(actorUserId, request, cancellationToken);
            return Created($"/api/v1/admin/remediation/{response.Id}",
                ApiEnvelope<RemediationActionResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException ex)
        {
            return BadRequest(BuildValidationProblem(ex, "Invalid remediation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPost("{remediationId:guid}/verify")]
    [ProducesResponseType(typeof(ApiEnvelope<RemediationActionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify(
        Guid remediationId,
        [FromBody] VerifyRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _remediationService.VerifyAsync(actorUserId, remediationId, request, cancellationToken);
            return Ok(ApiEnvelope<RemediationActionResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Verification failed", detail: ex.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPost("{remediationId:guid}/resolve")]
    [ProducesResponseType(typeof(ApiEnvelope<RemediationActionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Resolve(
        Guid remediationId,
        [FromBody] ResolveRemediationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _remediationService.ResolveAsync(actorUserId, remediationId, request, cancellationToken);
            return Ok(ApiEnvelope<RemediationActionResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Resolve failed", detail: ex.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("{remediationId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<RemediationActionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(
        Guid remediationId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _remediationService.GetByIdAsync(actorUserId, remediationId, cancellationToken);
            return Ok(ApiEnvelope<RemediationActionResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Not found", detail: ex.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException ex, string title)
    {
        var errors = ex.Errors.GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        return new ValidationProblemDetails(errors) { Status = StatusCodes.Status400BadRequest, Title = title };
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out actorUserId);
    }

    private object BuildMeta() => new { requestId = HttpContext.TraceIdentifier, timestampUtc = DateTime.UtcNow };
}
