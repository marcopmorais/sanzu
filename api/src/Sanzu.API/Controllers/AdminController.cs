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
[Route("api/v1/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ITenantLifecycleService _tenantLifecycleService;
    private readonly ISupportDiagnosticsService _supportDiagnosticsService;
    private readonly ITenantPolicyControlService _tenantPolicyControlService;

    public AdminController(
        ITenantLifecycleService tenantLifecycleService,
        ISupportDiagnosticsService supportDiagnosticsService,
        ITenantPolicyControlService tenantPolicyControlService)
    {
        _tenantLifecycleService = tenantLifecycleService;
        _supportDiagnosticsService = supportDiagnosticsService;
        _tenantPolicyControlService = tenantPolicyControlService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPatch("tenants/{tenantId:guid}/lifecycle")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantLifecycleStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateTenantLifecycleState(
        Guid tenantId,
        [FromBody] UpdateTenantLifecycleStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantLifecycleService.UpdateTenantLifecycleStateAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantLifecycleStateResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid tenant lifecycle request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantLifecycleStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenant lifecycle state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPost("tenants/{tenantId:guid}/diagnostics/sessions")]
    [ProducesResponseType(typeof(ApiEnvelope<SupportDiagnosticSessionResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartDiagnosticSession(
        Guid tenantId,
        [FromBody] StartSupportDiagnosticSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _supportDiagnosticsService.StartDiagnosticSessionAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/admin/tenants/{tenantId}/diagnostics/sessions/{response.SessionId}",
                ApiEnvelope<SupportDiagnosticSessionResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid support diagnostics request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (SupportDiagnosticAccessException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Support diagnostics conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("tenants/{tenantId:guid}/diagnostics/sessions/{sessionId:guid}/summary")]
    [ProducesResponseType(typeof(ApiEnvelope<SupportDiagnosticSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetDiagnosticSummary(
        Guid tenantId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _supportDiagnosticsService.GetDiagnosticSummaryAsync(
                tenantId,
                actorUserId,
                sessionId,
                cancellationToken);

            return Ok(ApiEnvelope<SupportDiagnosticSummaryResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (SupportDiagnosticAccessException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Support diagnostics conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpPut("tenants/{tenantId:guid}/policy-controls")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantPolicyControlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApplyTenantPolicyControl(
        Guid tenantId,
        [FromBody] ApplyTenantPolicyControlRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantPolicyControlService.ApplyTenantPolicyControlAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantPolicyControlResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid tenant policy control request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException validationException, string title)
    {
        var errors = validationException.Errors
            .GroupBy(
                x => string.IsNullOrWhiteSpace(x.PropertyName)
                    ? "request"
                    : x.PropertyName)
            .ToDictionary(
                x => x.Key,
                x => x.Select(e => e.ErrorMessage).Distinct().ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = title,
            Status = StatusCodes.Status400BadRequest
        };
    }

    private static Dictionary<string, object?> BuildMeta()
    {
        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTime.UtcNow
        };
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        actorUserId = Guid.Empty;
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(userIdValue, out actorUserId);
    }
}
