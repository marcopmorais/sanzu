using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/tenants/{tenantId:guid}")]
[Authorize(Policy = "AdminSupport")]
public sealed class AdminHealthHistoryController : ControllerBase
{
    private readonly ITenantHealthScoreRepository _healthScoreRepository;

    public AdminHealthHistoryController(ITenantHealthScoreRepository healthScoreRepository)
    {
        _healthScoreRepository = healthScoreRepository;
    }

    [HttpGet("health-history")]
    [ProducesResponseType(typeof(ApiEnvelope<HealthHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealthHistory(
        Guid tenantId,
        [FromQuery] string? period = "90d",
        CancellationToken cancellationToken = default)
    {
        var days = period switch
        {
            "30d" => 30,
            "60d" => 60,
            "90d" => 90,
            _ => 90
        };

        var history = await _healthScoreRepository.GetHistoryByTenantIdAsync(tenantId, days, cancellationToken);

        var dataPoints = history.Select(h => new HealthHistoryDataPoint
        {
            Date = h.ComputedAt,
            Score = h.OverallScore,
            HealthBand = h.HealthBand.ToString()
        }).ToList();

        var trend = ComputeTrend(history);

        var response = new HealthHistoryResponse
        {
            TenantId = tenantId,
            Period = period ?? "90d",
            Trend = trend,
            DataPoints = dataPoints
        };

        return Ok(ApiEnvelope<HealthHistoryResponse>.Success(response, BuildMeta()));
    }

    private static string ComputeTrend(IReadOnlyList<TenantHealthScore> history)
    {
        if (history.Count < 2)
            return "Stable";

        var cutoff14Days = DateTime.UtcNow.AddDays(-14);
        var recent = history.Where(h => h.ComputedAt >= cutoff14Days).ToList();
        var older = history.Where(h => h.ComputedAt < cutoff14Days).ToList();

        if (recent.Count == 0 || older.Count == 0)
            return "Stable";

        var recentAvg = recent.Average(h => h.OverallScore);
        var olderAvg = older.Average(h => h.OverallScore);
        var change = recentAvg - olderAvg;

        return change switch
        {
            > 5 => "Improving",
            < -5 => "Degrading",
            _ => "Stable"
        };
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}

public sealed class HealthHistoryResponse
{
    public Guid TenantId { get; init; }
    public string Period { get; init; } = "90d";
    public string Trend { get; init; } = "Stable";
    public IReadOnlyList<HealthHistoryDataPoint> DataPoints { get; init; } = [];
}

public sealed class HealthHistoryDataPoint
{
    public DateTime Date { get; init; }
    public int Score { get; init; }
    public string HealthBand { get; init; } = string.Empty;
}
