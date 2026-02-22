using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/alerts")]
[Authorize(Policy = "AdminViewer")]
public sealed class AdminAlertsController : ControllerBase
{
    private readonly IAdminAlertService _alertService;
    private readonly IOrganizationRepository _organizationRepository;

    public AdminAlertsController(
        IAdminAlertService alertService,
        IOrganizationRepository organizationRepository)
    {
        _alertService = alertService;
        _organizationRepository = organizationRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<AdminAlertResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] string? alertType = null,
        CancellationToken cancellationToken = default)
    {
        AlertStatus? parsedStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AlertStatus>(status, true, out var s))
            parsedStatus = s;

        AlertSeverity? parsedSeverity = null;
        if (!string.IsNullOrEmpty(severity) && Enum.TryParse<AlertSeverity>(severity, true, out var sev))
            parsedSeverity = sev;

        var alerts = await _alertService.GetAlertsAsync(parsedStatus, parsedSeverity, alertType, cancellationToken);

        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var orgLookup = allOrgs.ToDictionary(o => o.Id, o => o.Name);

        var response = alerts.Select(a => new AdminAlertResponse
        {
            Id = a.Id,
            TenantId = a.TenantId,
            AlertType = a.AlertType,
            Severity = a.Severity.ToString(),
            Title = a.Title,
            Detail = a.Detail,
            Status = a.Status.ToString(),
            RoutedToRole = a.RoutedToRole,
            OwnedByUserId = a.OwnedByUserId,
            FiredAt = a.FiredAt,
            AcknowledgedAt = a.AcknowledgedAt,
            ResolvedAt = a.ResolvedAt,
            TenantName = a.TenantId.HasValue
                ? orgLookup.GetValueOrDefault(a.TenantId.Value, "Unknown")
                : null
        }).ToList();

        return Ok(ApiEnvelope<IReadOnlyList<AdminAlertResponse>>.Success(response, BuildMeta()));
    }

    [HttpPatch("{alertId:guid}")]
    [Authorize(Policy = "AdminOps")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAlertStatus(
        Guid alertId,
        [FromBody] UpdateAlertStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var alert = await _alertService.GetAlertByIdAsync(alertId, cancellationToken);
        if (alert is null)
            return NotFound();

        switch (request.Status?.ToLowerInvariant())
        {
            case "acknowledged":
                await _alertService.AcknowledgeAlertAsync(alertId, actorUserId, cancellationToken);
                break;
            case "resolved":
                await _alertService.ResolveAlertAsync(alertId, actorUserId, cancellationToken);
                break;
            default:
                return BadRequest(new ProblemDetails { Title = "Invalid status", Detail = "Status must be 'Acknowledged' or 'Resolved'" });
        }

        return NoContent();
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

public sealed class UpdateAlertStatusRequest
{
    public string? Status { get; set; }
}
