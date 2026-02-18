using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/admin/recovery")]
public sealed class AdminRecoveryController : ControllerBase
{
    private readonly IRecoveryPlanService _recoveryPlanService;

    public AdminRecoveryController(IRecoveryPlanService recoveryPlanService)
    {
        _recoveryPlanService = recoveryPlanService;
    }

    [Authorize(Policy = "SanzuAdmin")]
    [HttpGet("{tenantId:guid}/cases/{caseId:guid}/plan")]
    [ProducesResponseType(typeof(ApiEnvelope<RecoveryPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecoveryPlan(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        try
        {
            var response = await _recoveryPlanService.GeneratePlatformRecoveryPlanAsync(actorUserId, tenantId, caseId, cancellationToken);
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
