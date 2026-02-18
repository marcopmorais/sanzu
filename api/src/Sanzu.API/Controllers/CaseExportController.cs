using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers;

[ApiController]
[Route("api/v1/tenants/{tenantId:guid}/cases/{caseId:guid}/export")]
public sealed class CaseExportController : ControllerBase
{
    private readonly ICaseAuditExportService _exportService;

    public CaseExportController(ICaseAuditExportService exportService)
    {
        _exportService = exportService;
    }

    [Authorize(Policy = "TenantAdmin")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<CaseAuditExportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ExportCaseAudit(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        try
        {
            var response = await _exportService.ExportAsync(
                tenantId,
                caseId,
                actorUserId,
                cancellationToken);

            return Ok(ApiEnvelope<CaseAuditExportResponse>.Success(response, BuildMeta()));
        }
        catch (CaseStateException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Export failed",
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
