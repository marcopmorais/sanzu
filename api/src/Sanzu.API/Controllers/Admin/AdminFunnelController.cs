using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Enums;
using Sanzu.Core.Models.Responses;
using Sanzu.Infrastructure.Data;

namespace Sanzu.API.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/analytics/funnel")]
[Authorize(Policy = "AdminViewer")]
public sealed class AdminFunnelController : ControllerBase
{
    private readonly SanzuDbContext _dbContext;

    public AdminFunnelController(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiEnvelope<FunnelResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFunnel(
        [FromQuery] string? cohort = null,
        [FromQuery] string? cohortValue = null,
        CancellationToken cancellationToken = default)
    {
        var orgsQuery = _dbContext.Organizations.IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrEmpty(cohort) && !string.IsNullOrEmpty(cohortValue))
        {
            var (start, end) = ParseCohort(cohort, cohortValue);
            if (start.HasValue && end.HasValue)
            {
                orgsQuery = orgsQuery.Where(o => o.CreatedAt >= start.Value && o.CreatedAt < end.Value);
            }
        }

        var orgs = await orgsQuery.ToListAsync(cancellationToken);

        var cases = await _dbContext.Cases.IgnoreQueryFilters()
            .AsNoTracking()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var caseCounts = cases.ToDictionary(c => c.TenantId, c => c.Count);

        var signup = orgs.Count;
        var onboardingDefaults = orgs.Count(o =>
            o.DefaultLocale is not null || o.DefaultTimeZone is not null || o.DefaultCurrency is not null);
        var onboardingComplete = orgs.Count(o => o.OnboardingCompletedAt.HasValue);
        var billingActive = orgs.Count(o =>
            o.SubscriptionActivatedAt.HasValue && o.SubscriptionPlan is not null);
        var firstCaseCreated = orgs.Count(o => caseCounts.ContainsKey(o.Id) && caseCounts[o.Id] >= 1);
        var activeUsage = orgs.Count(o => caseCounts.ContainsKey(o.Id) && caseCounts[o.Id] >= 3);

        var stages = new List<FunnelStage>
        {
            BuildStage("Signup", signup, signup),
            BuildStage("OnboardingDefaults", onboardingDefaults, signup),
            BuildStage("OnboardingComplete", onboardingComplete, onboardingDefaults),
            BuildStage("BillingActive", billingActive, onboardingComplete),
            BuildStage("FirstCaseCreated", firstCaseCreated, billingActive),
            BuildStage("ActiveUsage", activeUsage, firstCaseCreated)
        };

        var response = new FunnelResponse
        {
            Stages = stages,
            Cohort = cohort,
            CohortValue = cohortValue
        };

        return Ok(ApiEnvelope<FunnelResponse>.Success(response, BuildMeta()));
    }

    [HttpGet("stages/{stageName}/tenants")]
    [ProducesResponseType(typeof(ApiEnvelope<IReadOnlyList<FunnelTenantItem>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStageTenants(
        string stageName,
        CancellationToken cancellationToken)
    {
        var orgs = await _dbContext.Organizations.IgnoreQueryFilters()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var cases = await _dbContext.Cases.IgnoreQueryFilters()
            .AsNoTracking()
            .GroupBy(c => c.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var caseCounts = cases.ToDictionary(c => c.TenantId, c => c.Count);

        var now = DateTime.UtcNow;
        var filtered = stageName.ToLowerInvariant() switch
        {
            "signup" => orgs,
            "onboardingdefaults" => orgs.Where(o =>
                o.DefaultLocale is not null || o.DefaultTimeZone is not null || o.DefaultCurrency is not null).ToList(),
            "onboardingcomplete" => orgs.Where(o => o.OnboardingCompletedAt.HasValue).ToList(),
            "billingactive" => orgs.Where(o =>
                o.SubscriptionActivatedAt.HasValue && o.SubscriptionPlan is not null).ToList(),
            "firstcasecreated" => orgs.Where(o => caseCounts.ContainsKey(o.Id) && caseCounts[o.Id] >= 1).ToList(),
            "activeusage" => orgs.Where(o => caseCounts.ContainsKey(o.Id) && caseCounts[o.Id] >= 3).ToList(),
            _ => []
        };

        var items = filtered.Select(o => new FunnelTenantItem
        {
            TenantId = o.Id,
            Name = o.Name,
            SignupDate = o.CreatedAt,
            DaysAtStage = (int)(now - o.CreatedAt).TotalDays
        }).OrderByDescending(t => t.DaysAtStage).ToList();

        return Ok(ApiEnvelope<IReadOnlyList<FunnelTenantItem>>.Success(items, BuildMeta()));
    }

    private static FunnelStage BuildStage(string name, int count, int previousCount)
    {
        var dropOff = previousCount > 0 ? previousCount - count : 0;
        var dropOffPct = previousCount > 0 ? Math.Round((double)dropOff / previousCount * 100, 1) : 0;

        return new FunnelStage
        {
            StageName = name,
            Count = count,
            DropOffCount = dropOff,
            DropOffPercentage = dropOffPct
        };
    }

    private static (DateTime? start, DateTime? end) ParseCohort(string cohort, string value)
    {
        try
        {
            if (cohort == "month" && DateTime.TryParse(value + "-01", out var monthStart))
            {
                return (monthStart, monthStart.AddMonths(1));
            }
            if (cohort == "week" && DateTime.TryParse(value, out var weekStart))
            {
                return (weekStart, weekStart.AddDays(7));
            }
        }
        catch
        {
            // Invalid format
        }
        return (null, null);
    }

    private static Dictionary<string, object?> BuildMeta()
        => new() { ["timestamp"] = DateTime.UtcNow };
}

public sealed class FunnelResponse
{
    public IReadOnlyList<FunnelStage> Stages { get; init; } = [];
    public string? Cohort { get; init; }
    public string? CohortValue { get; init; }
}

public sealed class FunnelStage
{
    public string StageName { get; init; } = string.Empty;
    public int Count { get; init; }
    public int DropOffCount { get; init; }
    public double DropOffPercentage { get; init; }
}

public sealed class FunnelTenantItem
{
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime SignupDate { get; init; }
    public int DaysAtStage { get; init; }
}
