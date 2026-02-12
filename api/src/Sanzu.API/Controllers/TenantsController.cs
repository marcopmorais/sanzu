using FluentValidation;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants")]
public sealed class TenantsController : ControllerBase
{
    private readonly ITenantOnboardingService _tenantOnboardingService;

    public TenantsController(ITenantOnboardingService tenantOnboardingService)
    {
        _tenantOnboardingService = tenantOnboardingService;
    }

    [HttpPost("signup")]
    [ProducesResponseType(typeof(ApiEnvelope<CreateAgencyAccountResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup(
        [FromBody] CreateAgencyAccountRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _tenantOnboardingService.CreateAgencyAccountAsync(request, cancellationToken);
            var meta = new Dictionary<string, object?>
            {
                ["timestamp"] = DateTime.UtcNow
            };

            return Created(
                $"/api/v1/tenants/{response.OrganizationId}",
                ApiEnvelope<CreateAgencyAccountResponse>.Success(response, meta));
        }
        catch (ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(
                    x => string.IsNullOrWhiteSpace(x.PropertyName)
                        ? "request"
                        : x.PropertyName)
                .ToDictionary(
                    x => x.Key,
                    x => x.Select(e => e.ErrorMessage).Distinct().ToArray());

            var details = new ValidationProblemDetails(errors)
            {
                Title = "Invalid signup request",
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(details);
        }
        catch (DuplicateEmailException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Unable to create account",
                detail: "The account could not be created with the provided information.");
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("{tenantId:guid}/onboarding/profile")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOnboardingProfile(
        Guid tenantId,
        [FromBody] UpdateTenantOnboardingProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.UpdateOnboardingProfileAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingProfileResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding profile request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPatch("{tenantId:guid}/onboarding/defaults")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingDefaultsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOnboardingDefaults(
        Guid tenantId,
        [FromBody] UpdateTenantOnboardingDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.UpdateOnboardingDefaultsAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingDefaultsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding defaults request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/onboarding/invitations")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantInvitationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOnboardingInvitation(
        Guid tenantId,
        [FromBody] CreateTenantInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.CreateInvitationAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/onboarding/invitations/{response.InvitationId}",
                ApiEnvelope<TenantInvitationResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid tenant invitation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingConflictException)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Unable to create invitation",
                detail: "The invitation could not be created with the provided information.");
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
                detail: exception.Message);
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("{tenantId:guid}/onboarding/complete")]
    [ProducesResponseType(typeof(ApiEnvelope<TenantOnboardingCompletionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteOnboarding(
        Guid tenantId,
        [FromBody] CompleteTenantOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _tenantOnboardingService.CompleteOnboardingAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<TenantOnboardingCompletionResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid onboarding completion request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (TenantOnboardingStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Onboarding state conflict",
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
