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
[Route("api/v1/tenants/{tenantId:guid}/settings")]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly ITenantCaseDefaultsService _tenantCaseDefaultsService;

    public TenantSettingsController(ITenantCaseDefaultsService tenantCaseDefaultsService)
    {
        _tenantCaseDefaultsService = tenantCaseDefaultsService;
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet("case-defaults")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantCaseDefaultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCaseDefaults(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantCaseDefaultsService.GetCaseDefaultsAsync(
                tenantId,
                actorUserId,
                cancellationToken);

            return Ok(ApiEnvelope<TenantCaseDefaultsResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenant configuration state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("case-defaults")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantCaseDefaultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaseDefaults(
        Guid tenantId,
        [FromBody] UpdateTenantCaseDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantCaseDefaultsService.UpdateCaseDefaultsAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantCaseDefaultsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case defaults request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tenant configuration state conflict",
                detail: exception.Message);
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
