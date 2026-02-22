using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/revenue")]
[Authorize(Policy = "AdminFinance")]
public sealed class AdminRevenueController : ControllerBase
{
    private readonly IAdminRevenueService _revenueService;

    public AdminRevenueController(IAdminRevenueService revenueService)
    {
        _revenueService = revenueService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<RevenueOverviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var overview = await _revenueService.GetRevenueOverviewAsync(cancellationToken);
        return Ok(ApiEnvelope<RevenueOverviewResponse>.Success(overview, BuildMeta()));
    }

    [HttpGet("trends")]
    [ProducesResponseType(typeof(ApiEnvelope<RevenueTrendsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrends(
        [FromQuery] string period = "monthly",
        CancellationToken cancellationToken = default)
    {
        var trends = await _revenueService.GetRevenueTrendsAsync(period, cancellationToken);
        return Ok(ApiEnvelope<RevenueTrendsResponse>.Success(trends, BuildMeta()));
    }

    [HttpGet("billing-health")]
    [ProducesResponseType(typeof(ApiEnvelope<BillingHealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBillingHealth(CancellationToken cancellationToken)
    {
        var health = await _revenueService.GetBillingHealthAsync(cancellationToken);
        return Ok(ApiEnvelope<BillingHealthResponse>.Success(health, BuildMeta()));
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportRevenue(CancellationToken cancellationToken)
    {
        var rows = await _revenueService.GetRevenueExportDataAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("TenantName,PlanTier,MrrContribution,BillingStatus,LastPaymentDate,NextRenewal");
        foreach (var r in rows)
        {
            sb.AppendLine($"{CsvEscape(r.TenantName)},{CsvEscape(r.PlanTier)},{r.MrrContribution},{CsvEscape(r.BillingStatus)},{r.LastPaymentDate:yyyy-MM-dd},{r.NextRenewal:yyyy-MM-dd}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "revenue-export.csv");
    }

    [HttpGet("billing-health/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportBillingHealth(CancellationToken cancellationToken)
    {
        var rows = await _revenueService.GetBillingHealthExportDataAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("TenantName,IssueType,FailedAmount,LastFailedAt,GracePeriodRetryAt,NextRenewalDate");
        foreach (var r in rows)
        {
            sb.AppendLine($"{CsvEscape(r.TenantName)},{CsvEscape(r.IssueType)},{r.FailedAmount},{r.LastFailedAt:yyyy-MM-dd},{r.GracePeriodRetryAt:yyyy-MM-dd},{r.NextRenewalDate:yyyy-MM-dd}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "billing-health-export.csv");
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}
