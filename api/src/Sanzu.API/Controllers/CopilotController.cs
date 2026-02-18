using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/copilot")]
public sealed class CopilotController : ControllerBase
{
    private readonly ICopilotDraftService _copilotDraftService;
    private readonly IRecoveryPlanService _recoveryPlanService;

    public CopilotController(ICopilotDraftService copilotDraftService, IRecoveryPlanService recoveryPlanService)
    {
        _copilotDraftService = copilotDraftService;
        _recoveryPlanService = recoveryPlanService;
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("draft")]
    [ProducesResponseType(typeof(ApiEnvelope<CopilotDraftResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestDraft(
        Guid tenantId,
        [FromBody] RequestCopilotDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _copilotDraftService.GenerateDraftAsync(actorUserId, tenantId, request, cancellationToken);
            return Ok(ApiEnvelope<CopilotDraftResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Draft generation failed", detail: ex.Message);
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("draft/accept")]
    [ProducesResponseType(typeof(ApiEnvelope<CopilotDraftAcceptedResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AcceptDraft(
        Guid tenantId,
        [FromBody] AcceptCopilotDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _copilotDraftService.AcceptDraftAsync(actorUserId, tenantId, request, cancellationToken);
            return Ok(ApiEnvelope<CopilotDraftAcceptedResponse>.Success(response, BuildMeta()));
        }
        catch (TenantAccessDeniedException)
        {
            return Forbid();
        }
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpPost("recovery-plan")]
    [ProducesResponseType(typeof(ApiEnvelope<RecoveryPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestRecoveryPlan(
        Guid tenantId,
        [FromBody] RequestRecoveryPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _recoveryPlanService.GenerateRecoveryPlanAsync(actorUserId, tenantId, request, cancellationToken);
            return Ok(ApiEnvelope<RecoveryPlanResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Recovery plan failed", detail: ex.Message);
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

    private object BuildMeta() => new { requestId = HttpContext.TraceIdentifier, timestampUtc = DateTime.UtcNow };
}
