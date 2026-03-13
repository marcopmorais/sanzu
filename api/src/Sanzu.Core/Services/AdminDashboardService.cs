using System.Text.Json;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IAdminDashboardSnapshotRepository _snapshotRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ITenantHealthScoreRepository _healthScoreRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminRevenueService _revenueService;
    private readonly IAdminAlertRepository _alertRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AdminDashboardService(
        IAdminDashboardSnapshotRepository snapshotRepository,
        IOrganizationRepository organizationRepository,
        ITenantHealthScoreRepository healthScoreRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IAdminRevenueService revenueService,
        IAdminAlertRepository alertRepository)
    {
        _snapshotRepository = snapshotRepository;
        _organizationRepository = organizationRepository;
        _healthScoreRepository = healthScoreRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _revenueService = revenueService;
        _alertRepository = alertRepository;
    }

    public async Task ComputeSnapshotAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var summary = await ComputeSummaryAsync(cancellationToken);

        var snapshot = new AdminDashboardSnapshot
        {
            Id = Guid.NewGuid(),
            SnapshotType = "DashboardSummary",
            JsonPayload = JsonSerializer.Serialize(summary, JsonOptions),
            ComputedAt = now,
            ExpiresAt = now.AddMinutes(10)
        };

        await _snapshotRepository.UpsertAsync(snapshot, cancellationToken);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = Guid.Empty,
                EventType = "Admin.Dashboard.SnapshotComputed",
                Metadata = JsonSerializer.Serialize(new { snapshotType = "DashboardSummary", computedAt = now }, JsonOptions),
                CreatedAt = now
            }, ct);
        }, cancellationToken);
    }

    public async Task<AdminDashboardSummary?> GetLatestSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.GetLatestByTypeAsync("DashboardSummary", cancellationToken);
        if (snapshot is null)
            return null;

        return JsonSerializer.Deserialize<AdminDashboardSummary>(snapshot.JsonPayload, JsonOptions);
    }

    public async Task<DashboardResponse<AdminDashboardSummary>> GetDashboardAsync(int snapshotIntervalMinutes, CancellationToken cancellationToken)
    {
        var snapshot = await _snapshotRepository.GetLatestByTypeAsync("DashboardSummary", cancellationToken);

        if (snapshot is null)
        {
            var empty = new AdminDashboardSummary(
                DateTime.MinValue,
                new TenantCounts(0, 0, 0, 0, 0),
                new RevenuePulse(0m, 0m, 0m, 0m),
                new HealthDistribution(0, 0, 0, []),
                new AlertCounts(0, 0, 0, 0),
                new OnboardingStatus(0m, 0));

            return new DashboardResponse<AdminDashboardSummary>(empty, DateTime.MinValue, true);
        }

        var summary = JsonSerializer.Deserialize<AdminDashboardSummary>(snapshot.JsonPayload, JsonOptions)!;
        var age = DateTime.UtcNow - snapshot.ComputedAt;
        var isStale = age > TimeSpan.FromMinutes(snapshotIntervalMinutes * 2);

        return new DashboardResponse<AdminDashboardSummary>(summary, snapshot.ComputedAt, isStale);
    }

    private async Task<AdminDashboardSummary> ComputeSummaryAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var tenantCounts = await ComputeTenantCountsAsync(cancellationToken);
        var revenue = await ComputeRevenuePulseAsync(cancellationToken);
        var health = await ComputeHealthDistributionAsync(cancellationToken);
        var alerts = await ComputeAlertCountsAsync(cancellationToken);
        var onboarding = await ComputeOnboardingStatusAsync(cancellationToken);

        return new AdminDashboardSummary(now, tenantCounts, revenue, health, alerts, onboarding);
    }

    private async Task<TenantCounts> ComputeTenantCountsAsync(CancellationToken cancellationToken)
    {
        var allTenants = await _organizationRepository.GetAllAsync(cancellationToken);

        var active = allTenants.Count(t => t.Status == TenantStatus.Active);
        var trial = allTenants.Count(t => t.Status == TenantStatus.Onboarding || t.Status == TenantStatus.Pending);
        var churning = allTenants.Count(t => t.Status == TenantStatus.PaymentIssue);
        var suspended = allTenants.Count(t => t.Status == TenantStatus.Suspended);
        var total = active + trial + churning + suspended;

        return new TenantCounts(total, active, trial, churning, suspended);
    }

    private async Task<RevenuePulse> ComputeRevenuePulseAsync(CancellationToken cancellationToken)
    {
        var overview = await _revenueService.GetRevenueOverviewAsync(cancellationToken);
        return new RevenuePulse(overview.Mrr, overview.Arr, overview.ChurnRate, overview.GrowthRate);
    }

    private async Task<HealthDistribution> ComputeHealthDistributionAsync(CancellationToken cancellationToken)
    {
        var latestScores = await _healthScoreRepository.GetLatestForAllTenantsAsync(cancellationToken);
        var allTenants = await _organizationRepository.GetAllAsync(cancellationToken);
        var tenantLookup = allTenants.ToDictionary(t => t.Id, t => t.Name);

        var green = latestScores.Count(s => s.HealthBand == HealthBand.Green);
        var yellow = latestScores.Count(s => s.HealthBand == HealthBand.Yellow);
        var red = latestScores.Count(s => s.HealthBand == HealthBand.Red);

        var topAtRisk = latestScores
            .Where(s => s.HealthBand == HealthBand.Red)
            .OrderBy(s => s.OverallScore)
            .Take(5)
            .Select(s => new AtRiskTenant(
                s.TenantId,
                tenantLookup.GetValueOrDefault(s.TenantId, "Unknown"),
                s.OverallScore,
                s.PrimaryIssue))
            .ToList();

        return new HealthDistribution(green, yellow, red, topAtRisk);
    }

    private async Task<AlertCounts> ComputeAlertCountsAsync(CancellationToken cancellationToken)
    {
        var firedAlerts = await _alertRepository.GetAllAsync(Enums.AlertStatus.Fired, null, null, cancellationToken);
        var critical = firedAlerts.Count(a => a.Severity == Enums.AlertSeverity.Critical);
        var warning = firedAlerts.Count(a => a.Severity == Enums.AlertSeverity.Warning);
        var info = firedAlerts.Count(a => a.Severity == Enums.AlertSeverity.Info);
        var unacknowledged = firedAlerts.Count;

        return new AlertCounts(critical, warning, info, unacknowledged);
    }

    private async Task<OnboardingStatus> ComputeOnboardingStatusAsync(CancellationToken cancellationToken)
    {
        var allTenants = await _organizationRepository.GetAllAsync(cancellationToken);

        var totalSignups = allTenants.Count(t =>
            t.Status != TenantStatus.Terminated);

        var completedOnboarding = allTenants.Count(t =>
            t.OnboardingCompletedAt.HasValue);

        var completionRate = totalSignups > 0
            ? Math.Round((decimal)completedOnboarding / totalSignups * 100, 1)
            : 0m;

        var stalledCutoff = DateTime.UtcNow.AddDays(-14);
        var stalled = allTenants.Count(t =>
            t.Status == TenantStatus.Onboarding
            && t.CreatedAt < stalledCutoff);

        return new OnboardingStatus(completionRate, stalled);
    }
}
