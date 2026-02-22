using System.Globalization;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AdminRevenueService : IAdminRevenueService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IBillingRecordRepository _billingRecordRepository;

    public AdminRevenueService(
        IOrganizationRepository organizationRepository,
        IBillingRecordRepository billingRecordRepository)
    {
        _organizationRepository = organizationRepository;
        _billingRecordRepository = billingRecordRepository;
    }

    public async Task<RevenueOverviewResponse> GetRevenueOverviewAsync(CancellationToken cancellationToken)
    {
        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var activeOrgs = allOrgs.Where(o => o.Status == TenantStatus.Active).ToList();

        // MRR & Plan Breakdown
        var planGroups = activeOrgs
            .GroupBy(o => o.SubscriptionPlan ?? "Unknown")
            .Select(g => new
            {
                PlanName = g.Key,
                TenantCount = g.Count(),
                Mrr = g.Sum(o => GetMonthlyAmount(o.SubscriptionPlan))
            })
            .ToList();

        var totalMrr = planGroups.Sum(p => p.Mrr);

        var planBreakdown = planGroups
            .Select(p => new PlanRevenueItem(
                p.PlanName,
                p.TenantCount,
                p.Mrr,
                totalMrr > 0 ? Math.Round(p.Mrr / totalMrr * 100, 1) : 0m))
            .ToList();

        // Churn rate: terminated in last 30 days / active count at period start
        var recentlyTerminated = allOrgs.Count(o =>
            o.Status == TenantStatus.Terminated && o.UpdatedAt >= thirtyDaysAgo);
        var activeAtStart = activeOrgs.Count + recentlyTerminated; // approximate
        var churnRate = activeAtStart > 0
            ? Math.Round((decimal)recentlyTerminated / activeAtStart * 100, 1)
            : 0m;

        // Growth rate: new tenants in last 30 days (non-terminated) / active at start
        var newTenants = allOrgs.Count(o =>
            o.CreatedAt >= thirtyDaysAgo && o.Status != TenantStatus.Terminated);
        var growthRate = activeAtStart > 0
            ? Math.Round((decimal)newTenants / activeAtStart * 100, 1)
            : 0m;

        return new RevenueOverviewResponse
        {
            Mrr = totalMrr,
            Arr = totalMrr * 12,
            ChurnRate = churnRate,
            GrowthRate = growthRate,
            PlanBreakdown = planBreakdown
        };
    }

    public async Task<RevenueTrendsResponse> GetRevenueTrendsAsync(string period, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        DateTime periodStart;

        switch (period.ToLowerInvariant())
        {
            case "daily":
                periodStart = now.AddDays(-30);
                break;
            case "weekly":
                periodStart = now.AddDays(-84); // 12 weeks
                break;
            default: // monthly
                periodStart = now.AddMonths(-12);
                break;
        }

        var records = await _billingRecordRepository.GetAllInPeriodForPlatformAsync(periodStart, now, cancellationToken);

        var dataPoints = records
            .GroupBy(r => GetPeriodLabel(r.BillingCycleStart, period))
            .Select(g => new RevenueTrendPoint(
                g.Key,
                g.Sum(r => r.TotalAmount),
                g.Select(r => r.TenantId).Distinct().Count()))
            .OrderBy(p => p.PeriodLabel)
            .ToList();

        return new RevenueTrendsResponse { DataPoints = dataPoints };
    }

    public async Task<BillingHealthResponse> GetBillingHealthAsync(CancellationToken cancellationToken)
    {
        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // Failed payments
        var failedOrgs = allOrgs
            .Where(o => o.FailedPaymentAttempts > 0 && o.LastPaymentFailedAt != null)
            .Select(o => new BillingHealthTenantItem
            {
                TenantId = o.Id,
                TenantName = o.Name,
                FailedAmount = GetMonthlyAmount(o.SubscriptionPlan),
                LastFailedAt = o.LastPaymentFailedAt
            })
            .ToList();

        // Grace period
        var graceOrgs = allOrgs
            .Where(o => o.NextPaymentRetryAt != null && o.NextPaymentRetryAt > now)
            .Select(o => new BillingHealthTenantItem
            {
                TenantId = o.Id,
                TenantName = o.Name,
                GracePeriodRetryAt = o.NextPaymentRetryAt
            })
            .ToList();

        // Upcoming renewals (billing records ending in next 30 days)
        var billingRecords = await _billingRecordRepository.GetAllForPlatformAsync(cancellationToken);
        var renewalCutoff = now.AddDays(30);
        var orgLookup = allOrgs.ToDictionary(o => o.Id, o => o.Name);

        var upcomingRenewals = billingRecords
            .Where(r => r.BillingCycleEnd >= now && r.BillingCycleEnd <= renewalCutoff)
            .GroupBy(r => r.TenantId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(r => r.BillingCycleEnd).First();
                return new BillingHealthTenantItem
                {
                    TenantId = latest.TenantId,
                    TenantName = orgLookup.GetValueOrDefault(latest.TenantId, "Unknown"),
                    NextRenewalDate = latest.BillingCycleEnd
                };
            })
            .ToList();

        return new BillingHealthResponse
        {
            FailedPaymentCount = failedOrgs.Count,
            OverdueInvoiceCount = failedOrgs.Count, // overdue ≈ failed for now
            GracePeriodCount = graceOrgs.Count,
            FailedPayments = failedOrgs,
            GracePeriodTenants = graceOrgs,
            UpcomingRenewals = upcomingRenewals
        };
    }

    public async Task<IReadOnlyList<RevenueExportRow>> GetRevenueExportDataAsync(CancellationToken cancellationToken)
    {
        var allOrgs = await _organizationRepository.GetAllAsync(cancellationToken);
        var billingRecords = await _billingRecordRepository.GetAllForPlatformAsync(cancellationToken);
        var now = DateTime.UtcNow;

        var latestBillingByTenant = billingRecords
            .GroupBy(r => r.TenantId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.BillingCycleEnd).First());

        var activeOrgs = allOrgs.Where(o => o.Status != TenantStatus.Terminated);

        return activeOrgs.Select(o =>
        {
            var billingStatus = o.FailedPaymentAttempts > 0 ? "Failed"
                : o.NextPaymentRetryAt != null && o.NextPaymentRetryAt > now ? "GracePeriod"
                : "Current";

            latestBillingByTenant.TryGetValue(o.Id, out var latestBilling);

            return new RevenueExportRow
            {
                TenantName = o.Name,
                PlanTier = o.SubscriptionPlan ?? "Unknown",
                MrrContribution = GetMonthlyAmount(o.SubscriptionPlan),
                BillingStatus = billingStatus,
                LastPaymentDate = latestBilling?.CreatedAt,
                NextRenewal = latestBilling?.BillingCycleEnd
            };
        }).ToList();
    }

    public async Task<IReadOnlyList<BillingHealthExportRow>> GetBillingHealthExportDataAsync(CancellationToken cancellationToken)
    {
        var health = await GetBillingHealthAsync(cancellationToken);
        var rows = new List<BillingHealthExportRow>();

        foreach (var item in health.FailedPayments)
        {
            rows.Add(new BillingHealthExportRow
            {
                TenantName = item.TenantName,
                IssueType = "FailedPayment",
                FailedAmount = item.FailedAmount,
                LastFailedAt = item.LastFailedAt
            });
        }

        foreach (var item in health.GracePeriodTenants)
        {
            rows.Add(new BillingHealthExportRow
            {
                TenantName = item.TenantName,
                IssueType = "GracePeriod",
                GracePeriodRetryAt = item.GracePeriodRetryAt
            });
        }

        foreach (var item in health.UpcomingRenewals)
        {
            rows.Add(new BillingHealthExportRow
            {
                TenantName = item.TenantName,
                IssueType = "UpcomingRenewal",
                NextRenewalDate = item.NextRenewalDate
            });
        }

        return rows;
    }

    internal static decimal GetMonthlyAmount(string? plan) => plan switch
    {
        "Starter" => 149m,
        "Professional" => 399m,
        "Enterprise" => 0m,
        _ => 0m
    };

    private static string GetPeriodLabel(DateTime date, string period)
    {
        return period.ToLowerInvariant() switch
        {
            "daily" => date.ToString("yyyy-MM-dd"),
            "weekly" => $"{date.Year}-W{ISOWeek.GetWeekOfYear(date):D2}",
            _ => date.ToString("yyyy-MM")
        };
    }
}
