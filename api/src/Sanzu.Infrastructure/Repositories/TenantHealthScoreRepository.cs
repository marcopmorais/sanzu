using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class TenantHealthScoreRepository : ITenantHealthScoreRepository
{
    private readonly SanzuDbContext _dbContext;

    public TenantHealthScoreRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TenantHealthScore?> GetLatestByTenantIdAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        return _dbContext.TenantHealthScores
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ComputedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantHealthScore>> GetLatestForAllTenantsAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.TenantHealthScores
            .AsNoTracking()
            .GroupBy(x => x.TenantId)
            .Select(g => g.OrderByDescending(x => x.ComputedAt).First())
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(TenantHealthScore score, CancellationToken cancellationToken)
    {
        _dbContext.TenantHealthScores.Add(score);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken)
    {
        var oldScores = await _dbContext.TenantHealthScores
            .Where(x => x.ComputedAt < cutoff)
            .ToListAsync(cancellationToken);

        if (oldScores.Count > 0)
        {
            _dbContext.TenantHealthScores.RemoveRange(oldScores);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
