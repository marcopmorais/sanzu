using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/admin/kpi")]
public sealed class KpiController : ControllerBase
{
    private readonly IKpiDashboardService _kpiDashboardService;

    public KpiController(IKpiDashboardService kpiDashboardService)
    {
        _kpiDashboardService = kpiDashboardService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(ApiEnvelope<PlatformKpiDashboardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] int periodDays = 30,
        [FromQuery] int tenantLimit = 10,
        [FromQuery] int caseLimit = 10,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _kpiDashboardService.GetDashboardAsync(
                actorUserId,
                periodDays,
                tenantLimit,
                caseLimit,
                cancellationToken);

            return Ok(ApiEnvelope<PlatformKpiDashboardResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid KPI dashboard request"));
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
