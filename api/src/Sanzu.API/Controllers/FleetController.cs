using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/admin/fleet")]
public sealed class FleetController : ControllerBase
{
    private readonly IFleetPostureService _fleetPostureService;

    public FleetController(IFleetPostureService fleetPostureService)
    {
        _fleetPostureService = fleetPostureService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<FleetPostureResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetFleetPosture(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _fleetPostureService.GetFleetPostureAsync(
                actorUserId, search, status, cancellationToken);
            return Ok(ApiEnvelope<FleetPostureResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantDrilldownResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetTenantDrilldown(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _fleetPostureService.GetTenantDrilldownAsync(
                actorUserId, tenantId, cancellationToken);
            return Ok(ApiEnvelope<TenantDrilldownResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Fleet drilldown failed",
                detail: exception.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
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
