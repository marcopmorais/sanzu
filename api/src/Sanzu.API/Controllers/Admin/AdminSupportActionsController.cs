using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tenants/{tenantId:guid}/actions")]
public sealed class AdminSupportActionsController : ControllerBase
{
    private readonly ISupportActionsService _supportActionsService;

    public AdminSupportActionsController(ISupportActionsService supportActionsService)
    {
        _supportActionsService = supportActionsService;
    }

    [HttpPost("override-blocked-step")]
    [Authorize(Policy = "AdminSupport")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OverrideBlockedStep(
        Guid tenantId,
        [FromBody] OverrideBlockedStepRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Rationale))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Rationale is required" });

        try
        {
            await _supportActionsService.OverrideBlockedStepAsync(
                tenantId, request.CaseId, request.StepId, request.Rationale, actorUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("extend-grace-period")]
    [Authorize(Policy = "AdminFinance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExtendGracePeriod(
        Guid tenantId,
        [FromBody] ExtendGracePeriodRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Rationale))
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Rationale is required" });

        if (request.Days < 1 || request.Days > 90)
            return BadRequest(new ProblemDetails { Title = "Validation Error", Detail = "Days must be between 1 and 90" });

        try
        {
            await _supportActionsService.ExtendGracePeriodAsync(
                tenantId, request.Days, request.Rationale, actorUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("re-onboard")]
    [Authorize(Policy = "AdminOps")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TriggerReOnboarding(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            await _supportActionsService.TriggerReOnboardingAsync(tenantId, actorUserId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message });
        }
    }

    [HttpPost("impersonate")]
    [Authorize(Policy = "AdminSupport")]
    [ProducesResponseType(typeof(ApiEnvelope<ImpersonationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartImpersonation(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var result = await _supportActionsService.StartImpersonationAsync(tenantId, actorUserId, cancellationToken);
            var response = new ImpersonationResponse
            {
                Token = result.Token,
                ExpiresAt = result.ExpiresAt,
                TenantId = result.TenantId,
                TenantName = result.TenantName
            };
            return Ok(ApiEnvelope<ImpersonationResponse>.Success(response, BuildMeta()));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = ex.Message });
        }
    }

    private bool TryGetActorUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)
                    ?? User.FindFirst("sub")
                    ?? User.FindFirst("user_id");
        return claim != null && Guid.TryParse(claim.Value, out userId);
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}

public sealed class OverrideBlockedStepRequest
{
    public Guid CaseId { get; set; }
    public Guid StepId { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public sealed class ExtendGracePeriodRequest
{
    public int Days { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

public sealed class ImpersonationResponse
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
}
