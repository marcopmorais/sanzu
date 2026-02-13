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
    [HttpPost("{caseId:guid}/documents")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDocumentUploadResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadCaseDocument(
        Guid tenantId,
        Guid caseId,
        [FromBody] UploadCaseDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UploadCaseDocumentAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/cases/{caseId}/documents/{response.DocumentId}",
                ApiEnvelope<CaseDocumentUploadResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case document upload request"));
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
    [HttpGet("{caseId:guid}/documents/{documentId:guid}")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDocumentDownloadResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadCaseDocument(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.DownloadCaseDocumentAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDocumentDownloadResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/documents/{documentId:guid}/versions")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDocumentUploadResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadCaseDocumentVersion(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        [FromBody] UploadCaseDocumentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UploadCaseDocumentVersionAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                request,
                cancellationToken);

            return Created(
                $"/api/v1/tenants/{tenantId}/cases/{caseId}/documents/{documentId}/versions/{response.VersionNumber}",
                ApiEnvelope<CaseDocumentUploadResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case document version upload request"));
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
    [HttpGet("{caseId:guid}/documents/{documentId:guid}/versions")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDocumentVersionHistoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCaseDocumentVersions(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GetCaseDocumentVersionsAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDocumentVersionHistoryResponse>.Success(response, BuildMeta()));
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
    [HttpPatch("{caseId:guid}/documents/{documentId:guid}/classification")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseDocumentClassificationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaseDocumentClassification(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        [FromBody] UpdateCaseDocumentClassificationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateCaseDocumentClassificationAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseDocumentClassificationResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid case document classification update request"));
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
    [HttpPost("{caseId:guid}/documents/templates/generate")]
    [ProducesResponseType(typeof(ApiEnvelope<GenerateOutboundTemplateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateOutboundTemplate(
        Guid tenantId,
        Guid caseId,
        [FromBody] GenerateOutboundTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.GenerateOutboundTemplateAsync(
                tenantId,
                actorUserId,
                caseId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<GenerateOutboundTemplateResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid outbound template generation request"));
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
    [HttpPost("{caseId:guid}/documents/{documentId:guid}/extraction/candidates")]
    [ProducesResponseType(typeof(ApiEnvelope<ExtractDocumentCandidatesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExtractDocumentCandidates(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.ExtractDocumentCandidatesAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                cancellationToken);

            return Ok(ApiEnvelope<ExtractDocumentCandidatesResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/documents/{documentId:guid}/extraction/review")]
    [ProducesResponseType(typeof(ApiEnvelope<ApplyExtractionDecisionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyExtractionDecisions(
        Guid tenantId,
        Guid caseId,
        Guid documentId,
        [FromBody] ApplyExtractionDecisionsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.ApplyExtractionDecisionsAsync(
                tenantId,
                actorUserId,
                caseId,
                documentId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<ApplyExtractionDecisionsResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid extraction decisions review request"));
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
    [HttpPost("{caseId:guid}/handoffs/packet")]
    [ProducesResponseType(typeof(ApiEnvelope<GenerateCaseHandoffPacketResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GenerateCaseHandoffPacket(
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
            var response = await _caseService.GenerateCaseHandoffPacketAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<GenerateCaseHandoffPacketResponse>.Success(response, BuildMeta()));
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
    [HttpGet("{caseId:guid}/handoffs/state")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseHandoffStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetCaseHandoffState(
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
            var response = await _caseService.GetCaseHandoffStateAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseHandoffStateResponse>.Success(response, BuildMeta()));
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
    [HttpPatch("{caseId:guid}/handoffs/{handoffId:guid}/state")]
    [ProducesResponseType(typeof(ApiEnvelope<CaseHandoffStateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateCaseHandoffState(
        Guid tenantId,
        Guid caseId,
        Guid handoffId,
        [FromBody] UpdateCaseHandoffStateRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _caseService.UpdateCaseHandoffStateAsync(
                tenantId,
                actorUserId,
                caseId,
                handoffId,
                request,
                cancellationToken);

            return Ok(ApiEnvelope<CaseHandoffStateResponse>.Success(response, BuildMeta()));
        }
        catch (ValidationException validationException)
        {
            return BadRequest(BuildValidationProblem(validationException, "Invalid handoff state update request"));
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
    [HttpGet("{caseId:guid}/process-alias")]
    [ProducesResponseType(typeof(ApiEnvelope<ProcessAliasResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetProcessAlias(
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
            var response = await _caseService.GetProcessAliasAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<ProcessAliasResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/process-alias/provision")]
    [ProducesResponseType(typeof(ApiEnvelope<ProcessAliasResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ProvisionProcessAlias(
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
            var response = await _caseService.ProvisionProcessAliasAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<ProcessAliasResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/process-alias/rotate")]
    [ProducesResponseType(typeof(ApiEnvelope<ProcessAliasResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RotateProcessAlias(
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
            var response = await _caseService.RotateProcessAliasAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<ProcessAliasResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/process-alias/deactivate")]
    [ProducesResponseType(typeof(ApiEnvelope<ProcessAliasResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateProcessAlias(
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
            var response = await _caseService.DeactivateProcessAliasAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<ProcessAliasResponse>.Success(response, BuildMeta()));
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
    [HttpPost("{caseId:guid}/process-alias/archive")]
    [ProducesResponseType(typeof(ApiEnvelope<ProcessAliasResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchiveProcessAlias(
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
            var response = await _caseService.ArchiveProcessAliasAsync(
                tenantId,
                actorUserId,
                caseId,
                cancellationToken);

            return Ok(ApiEnvelope<ProcessAliasResponse>.Success(response, BuildMeta()));
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
