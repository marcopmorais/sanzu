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
[Route("api/v1/tenants/{tenantId:guid}/cases/{caseId:guid}/participants")]
public sealed class CaseParticipantsController : ControllerBase
{
    private readonly ICaseService _caseService;

    public CaseParticipantsController(ICaseService caseService)
    {
        _caseService = caseService;
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<InviteCaseParticipantResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> InviteParticipant(
        Guid tenantId,
        Guid caseId,
        [FromBody] InviteCaseParticipantRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.InviteCaseParticipantAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/cases/{caseId}/participants/{response.Participant.ParticipantId}",
                ApiEnvelope<InviteCaseParticipantResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid participant invitation request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Case state conflict",
                detail: exception.Message);
        }
    }

    [Authorize]
    [HttpPost("{participantId:guid}/accept")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(
        Guid tenantId,
        Guid caseId,
        Guid participantId,
        [FromBody] AcceptCaseParticipantInvitationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.AcceptCaseParticipantInvitationAsync(
                tenantId,
                actorUserId,
                caseId,
                participantId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseParticipantResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid participant acceptance request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Case state conflict",
                detail: exception.Message);
        }
    }

    [Authorize]
    [HttpPatch("{participantId:guid}/role")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseParticipantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParticipantRole(
        Guid tenantId,
        Guid caseId,
        Guid participantId,
        [FromBody] UpdateCaseParticipantRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateCaseParticipantRoleAsync(
                tenantId,
                actorUserId,
                caseId,
                participantId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseParticipantResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid participant role update request"));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Case state conflict",
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
