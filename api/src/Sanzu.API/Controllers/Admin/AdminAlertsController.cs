using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/alerts")]
[Authorize(Policy = "AdminViewer")]
public sealed class AdminAlertsController : ControllerBase
{
    private readonly IAdminAlertService _alertService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly SanzuDbContext _dbContext;

    public AdminAlertsController(
        IAdminAlertService alertService,
        IOrganizationRepository organizationRepository,
        SanzuDbContext dbContext)
    {
        _alertService = alertService;
        _organizationRepository = organizationRepository;
        _dbContext = dbContext;
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

    [HttpPost("manual")]
    [Authorize(Policy = "AdminOps")]
    [ProducesResponseType(typeof(ApiEnvelope<AdminAlertResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateManualAlert(
        [FromBody] CreateManualAlertRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
            return Unauthorized();

        var alert = await _alertService.CreateManualAlertAsync(
            request.TenantId, request.Note ?? "", request.DueDate, actorUserId, cancellationToken);

        string? tenantName = null;
        if (alert.TenantId.HasValue)
        {
            var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
            tenantName = allOrgs.FirstOrDefault(o => o.Id == alert.TenantId.Value)?.Name;
        }

        var response = new AdminAlertResponse
        {
            Id = alert.Id,
            TenantId = alert.TenantId,
            AlertType = alert.AlertType,
            Severity = alert.Severity.ToString(),
            Title = alert.Title,
            Detail = alert.Detail,
            Status = alert.Status.ToString(),
            RoutedToRole = alert.RoutedToRole,
            FiredAt = alert.FiredAt,
            TenantName = tenantName
        };

        return Created($"/api/v1/admin/alerts/{alert.Id}",
            ApiEnvelope<AdminAlertResponse>.Success(response, BuildMeta()));
    }

    [HttpPost("delivery-config")]
    [Authorize(Policy = "AdminFull")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ConfigureDelivery(
        [FromBody] ConfigureDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AlertDeliveryConfigs
            .FirstOrDefaultAsync(c => c.Channel == request.Channel, cancellationToken);

        if (existing != null)
        {
            existing.Target = request.Target ?? "";
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.AlertDeliveryConfigs.Add(new AlertDeliveryConfig
            {
                Id = Guid.NewGuid(),
                Channel = request.Channel ?? "",
                Target = request.Target ?? "",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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

public sealed class CreateManualAlertRequest
{
    public Guid? TenantId { get; set; }
    public string? Note { get; set; }
    public DateTime DueDate { get; set; }
}

public sealed class ConfigureDeliveryRequest
{
    public string? Channel { get; set; }
    public string? Target { get; set; }
}
