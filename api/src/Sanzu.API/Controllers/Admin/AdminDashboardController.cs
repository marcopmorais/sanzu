using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Policy = "AdminViewer")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;
    private readonly int _snapshotIntervalMinutes;

    public AdminDashboardController(
        IAdminDashboardService dashboardService,
        IConfiguration configuration)
    {
        _dashboardService = dashboardService;
        _snapshotIntervalMinutes = configuration.GetValue("Dashboard:SnapshotIntervalMinutes", 5);
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiEnvelope<DashboardResponse<AdminDashboardSummary>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(_snapshotIntervalMinutes, cancellationToken);

        return Ok(ApiEnvelope<DashboardResponse<AdminDashboardSummary>>.Success(
            dashboard,
            BuildMeta()));
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}
