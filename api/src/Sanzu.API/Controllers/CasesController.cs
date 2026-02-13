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
[Route("api/v1/tenants/{tenantId:guid}/cases")]
public sealed class CasesController : ControllerBase
{
    private readonly ICaseService _caseService;

    public CasesController(ICaseService caseService)
    {
        _caseService = caseService;
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiEnvelope<CreateCaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateCase(
        Guid tenantId,
        [FromBody] CreateCaseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.CreateCaseAsync(
                tenantId,
                actorUserId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/cases/{response.CaseId}",
                ApiEnvelope<CreateCaseResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case creation request"));
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
    [HttpGet("{caseId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCaseDetails(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GetCaseDetailsAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDetailsResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpPatch("{caseId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaseDetails(
        Guid tenantId,
        Guid caseId,
        [FromBody] UpdateCaseDetailsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateCaseDetailsAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDetailsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case update request"));
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
    [HttpPut("{caseId:guid}/intake")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitCaseIntake(
        Guid tenantId,
        Guid caseId,
        [FromBody] SubmitCaseIntakeRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.SubmitCaseIntakeAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDetailsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid intake submission request"));
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
    [HttpPatch("{caseId:guid}/lifecycle")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDetailsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaseLifecycle(
        Guid tenantId,
        Guid caseId,
        [FromBody] UpdateCaseLifecycleRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateCaseLifecycleAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDetailsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case lifecycle update request"));
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
    [HttpPost("{caseId:guid}/plan/generate")]
    [ProducesResponseType(typeof(ApiEnvelope<GenerateCasePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateCasePlan(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GenerateCasePlanAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<GenerateCasePlanResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/plan/readiness/recalculate")]
    [ProducesResponseType(typeof(ApiEnvelope<GenerateCasePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecalculateCasePlanReadiness(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.RecalculateCasePlanReadinessAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<GenerateCasePlanResponse>.Success(response, BuildMeta()));
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
    [HttpPatch("{caseId:guid}/plan/steps/{stepId:guid}/readiness-override")]
    [ProducesResponseType(typeof(ApiEnvelope<GenerateCasePlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OverrideWorkflowStepReadiness(
        Guid tenantId,
        Guid caseId,
        Guid stepId,
        [FromBody] OverrideWorkflowStepReadinessRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.OverrideWorkflowStepReadinessAsync(
                tenantId,
                actorUserId,
                caseId,
                stepId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<GenerateCasePlanResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid readiness override request"));
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
    [HttpGet("{caseId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseTaskWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCaseTaskWorkspace(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GetCaseTaskWorkspaceAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseTaskWorkspaceResponse>.Success(response, BuildMeta()));
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
    [HttpPatch("{caseId:guid}/tasks/{stepId:guid}/status")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseTaskWorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWorkflowTaskStatus(
        Guid tenantId,
        Guid caseId,
        Guid stepId,
        [FromBody] UpdateWorkflowTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateWorkflowTaskStatusAsync(
                tenantId,
                actorUserId,
                caseId,
                stepId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseTaskWorkspaceResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid workflow task status update request"));
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
    [HttpGet("{caseId:guid}/timeline")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseTimelineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCaseTimeline(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GetCaseTimelineAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseTimelineResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize]
    [HttpGet("{caseId:guid}/milestones")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseMilestonesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCaseMilestones(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GetCaseMilestonesAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseMilestonesResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
        catch (CaseAccessDeniedException)
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
