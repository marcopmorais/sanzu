using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class AdminDashboardSnapshotRepository : IAdminDashboardSnapshotRepository
{
    private readonly SanzuDbContext _dbContext;

    public AdminDashboardSnapshotRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AdminDashboardSnapshot?> GetLatestByTypeAsync(
        string snapshotType,
        CancellationToken cancellationToken)
    {
        return _dbContext.AdminDashboardSnapshots
            .AsNoTracking()
            .Where(s => s.SnapshotType == snapshotType)
            .OrderByDescending(s => s.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(
        AdminDashboardSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var existing = await _dbContext.AdminDashboardSnapshots
            .FirstOrDefaultAsync(s => s.SnapshotType == snapshot.SnapshotType, cancellationToken);

        if (existing is not null)
        {
            existing.JsonPayload = snapshot.JsonPayload;
            existing.ComputedAt = snapshot.ComputedAt;
            existing.ExpiresAt = snapshot.ExpiresAt;
        }
        else
        {
            _dbContext.AdminDashboardSnapshots.Add(snapshot);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
