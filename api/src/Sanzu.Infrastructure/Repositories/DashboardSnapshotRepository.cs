using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class DashboardSnapshotRepository : IDashboardSnapshotRepository
{
    private readonly SanzuDbContext _dbContext;

    public DashboardSnapshotRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DashboardSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default)
        => _dbContext.DashboardSnapshots
            .AsNoTracking()
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<DashboardSnapshot> CreateOrUpdateAsync(
        DashboardSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.DashboardSnapshots
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            _dbContext.DashboardSnapshots.Add(snapshot);
        }
        else
        {
            existing.ComputedAt = snapshot.ComputedAt;
            existing.IsStale = snapshot.IsStale;
            existing.TotalTenants = snapshot.TotalTenants;
            existing.ActiveTenants = snapshot.ActiveTenants;
            existing.GreenTenants = snapshot.GreenTenants;
            existing.YellowTenants = snapshot.YellowTenants;
            existing.RedTenants = snapshot.RedTenants;
            existing.TotalRevenueMtd = snapshot.TotalRevenueMtd;
            existing.OpenAlerts = snapshot.OpenAlerts;
            existing.AvgHealthScore = snapshot.AvgHealthScore;
            existing.Metadata = snapshot.Metadata;
            existing.UpdatedAt = snapshot.UpdatedAt;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing ?? snapshot;
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        var all = await _dbContext.DashboardSnapshots.ToListAsync(cancellationToken);
        if (all.Count > 0)
        {
            _dbContext.DashboardSnapshots.RemoveRange(all);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
