using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/me")]
public sealed class AdminPermissionsController : ControllerBase
{
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminPermissionsController(IAuditRepository auditRepository, IUnitOfWork unitOfWork)
    {
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    [Authorize(Policy = "AdminViewer")]
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(ApiEnvelope<AdminPermissionsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPermissions(CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var permissions = BuildPermissionsForRole(role);

        await _unitOfWork.ExecuteInTransactionAsync(
            async ct =>
            {
                await _auditRepository.CreateAsync(
                    new Core.Entities.AuditEvent
                    {
                        Id = Guid.NewGuid(),
                        ActorUserId = actorUserId,
                        EventType = "Admin.Permissions.Accessed",
                        Metadata = $"{{\"role\":\"{role}\"}}",
                        CreatedAt = DateTime.UtcNow
                    },
                    ct);
            },
            cancellationToken);

        return Ok(ApiEnvelope<AdminPermissionsResponse>.Success(permissions, BuildMeta()));
    }

    private static AdminPermissionsResponse BuildPermissionsForRole(string role)
    {
        return role switch
        {
            nameof(PlatformRole.SanzuAdmin) => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: new[]
                {
                    "/admin/dashboard/*",
                    "/admin/tenants",
                    "/admin/tenants/{id}/summary",
                    "/admin/tenants/{id}/billing",
                    "/admin/tenants/{id}/cases",
                    "/admin/tenants/{id}/activity",
                    "/admin/tenants/{id}/comms",
                    "/admin/tenants/{id}/actions/*",
                    "/admin/alerts",
                    "/admin/audit",
                    "/admin/revenue",
                    "/admin/config/*",
                    "/admin/team",
                    "/admin/platform/*"
                },
                AccessibleWidgets: new[]
                {
                    "TenantSummary",
                    "HealthOverview",
                    "TopAtRisk",
                    "AlertInbox",
                    "OnboardingStatus",
                    "RevenuePulse"
                },
                CanTakeActions: true),

            nameof(PlatformRole.SanzuOps) => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: new[]
                {
                    "/admin/dashboard/*",
                    "/admin/tenants",
                    "/admin/tenants/{id}/summary",
                    "/admin/tenants/{id}/cases",
                    "/admin/tenants/{id}/activity",
                    "/admin/tenants/{id}/comms",
                    "/admin/tenants/{id}/actions/*",
                    "/admin/alerts",
                    "/admin/audit"
                },
                AccessibleWidgets: new[]
                {
                    "TenantSummary",
                    "HealthOverview",
                    "TopAtRisk",
                    "AlertInbox",
                    "OnboardingStatus"
                },
                CanTakeActions: true),

            nameof(PlatformRole.SanzuFinance) => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: new[]
                {
                    "/admin/dashboard/*",
                    "/admin/tenants",
                    "/admin/tenants/{id}/summary",
                    "/admin/tenants/{id}/billing",
                    "/admin/alerts",
                    "/admin/audit",
                    "/admin/revenue"
                },
                AccessibleWidgets: new[]
                {
                    "TenantSummary",
                    "RevenuePulse",
                    "HealthOverview"
                },
                CanTakeActions: true),

            nameof(PlatformRole.SanzuSupport) => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: new[]
                {
                    "/admin/dashboard/*",
                    "/admin/tenants",
                    "/admin/tenants/{id}/summary",
                    "/admin/tenants/{id}/cases",
                    "/admin/tenants/{id}/activity",
                    "/admin/tenants/{id}/comms",
                    "/admin/alerts"
                },
                AccessibleWidgets: new[]
                {
                    "TenantSummary",
                    "AlertInbox"
                },
                CanTakeActions: true),

            nameof(PlatformRole.SanzuViewer) => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: new[]
                {
                    "/admin/dashboard/*",
                    "/admin/tenants",
                    "/admin/tenants/{id}/summary",
                    "/admin/alerts"
                },
                AccessibleWidgets: new[]
                {
                    "TenantSummary",
                    "HealthOverview"
                },
                CanTakeActions: false),

            _ => new AdminPermissionsResponse(
                Role: role,
                AccessibleEndpoints: Array.Empty<string>(),
                AccessibleWidgets: Array.Empty<string>(),
                CanTakeActions: false)
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
