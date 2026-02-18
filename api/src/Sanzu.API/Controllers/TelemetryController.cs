using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class TelemetryController : ControllerBase
{
    private readonly ITrustTelemetryService _telemetryService;

    public TelemetryController(ITrustTelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet("tenants/{tenantId:guid}/telemetry")]
    [ProducesResponseType(typeof(ApiEnvelope<TrustTelemetryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTenantTelemetry(
        Guid tenantId,
        [FromQuery] int periodDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _telemetryService.GetTenantTelemetryAsync(
                tenantId,
                actorUserId,
                periodDays,
                cancellationToken);

            return Ok(ApiEnvelope<TrustTelemetryResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid telemetry request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("admin/telemetry")]
    [ProducesResponseType(typeof(ApiEnvelope<TrustTelemetryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlatformTelemetry(
        [FromQuery] int periodDays = 30,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _telemetryService.GetPlatformTelemetryAsync(
                actorUserId,
                periodDays,
                cancellationToken);

            return Ok(ApiEnvelope<TrustTelemetryResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid telemetry request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    private static ValidationProblemDetails BuildValidationProblem(ValidationException validationException, string title)
    {
        var errors = validationException.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = title
        };
    }

    private bool TryGetActorUserId(out Guid actorUserId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out actorUserId);
    }

    private object BuildMeta()
    {
        return new
        {
            requestId = HttpContext.TraceIdentifier,
            timestampUtc = DateTime.UtcNow
        };
    }
}
