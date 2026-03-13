using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Services;

public sealed class DashboardSnapshotService : IDashboardSnapshotService
{
    private static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ActiveTenantWindow = TimeSpan.FromDays(30);

    private readonly SanzuDbContext _dbContext;
    private readonly IDashboardSnapshotRepository _repository;

    public DashboardSnapshotService(SanzuDbContext dbContext, IDashboardSnapshotRepository repository)
    {
        _dbContext = dbContext;
        _repository = repository;
    }

    public async Task<DashboardSnapshot> ComputeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var activeCutoff = now - ActiveTenantWindow;

        var totalTenants = await _dbContext.Organizations
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);

        var activeTenants = await _dbContext.TenantHealthScores
            .IgnoreQueryFilters()
            .Where(h => h.ComputedAt >= activeCutoff)
            .Select(h => h.TenantId)
            .Distinct()
            .CountAsync(cancellationToken);

        var allLatestScores = await _dbContext.TenantHealthScores
            .IgnoreQueryFilters()
            .GroupBy(h => h.TenantId)
            .Select(g => g.OrderByDescending(h => h.ComputedAt).First())
            .ToListAsync(cancellationToken);

        var greenTenants = allLatestScores.Count(s => s.HealthBand == HealthBand.Green);
        var yellowTenants = allLatestScores.Count(s => s.HealthBand == HealthBand.Yellow);
        var redTenants = allLatestScores.Count(s => s.HealthBand == HealthBand.Red);
        var avgHealthScore = allLatestScores.Count > 0
            ? (decimal)allLatestScores.Average(s => s.OverallScore)
            : 0m;

        var totalRevenueMtd = await _dbContext.BillingRecords
            .IgnoreQueryFilters()
            .Where(b => b.BillingCycleStart >= monthStart)
            .SumAsync(b => (decimal?)b.TotalAmount, cancellationToken) ?? 0m;

        var openAlerts = await _dbContext.KpiAlerts
            .IgnoreQueryFilters()
            .CountAsync(cancellationToken);

        return new DashboardSnapshot
        {
            Id = Guid.NewGuid(),
            ComputedAt = now,
            IsStale = false,
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            GreenTenants = greenTenants,
            YellowTenants = yellowTenants,
            RedTenants = redTenants,
            TotalRevenueMtd = totalRevenueMtd,
            OpenAlerts = openAlerts,
            AvgHealthScore = avgHealthScore,
            Metadata = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public Task<DashboardSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default)
        => _repository.GetLatestAsync(cancellationToken);

    public async Task<bool> IsStaleAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _repository.GetLatestAsync(cancellationToken);
        if (snapshot is null)
            return true;

        return DateTime.UtcNow - snapshot.ComputedAt > StalenessThreshold;
    }
}
