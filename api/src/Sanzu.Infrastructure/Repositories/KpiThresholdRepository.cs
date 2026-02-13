using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class KpiThresholdRepository : IKpiThresholdRepository
{
    private readonly SanzuDbContext _dbContext;

    public KpiThresholdRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<KpiThresholdDefinition?> GetByMetricAsync(KpiMetricKey metricKey, CancellationToken cancellationToken)
    {
        return _dbContext.KpiThresholds.FirstOrDefaultAsync(x => x.MetricKey == metricKey, cancellationToken);
    }

    public async Task<IReadOnlyList<KpiThresholdDefinition>> GetEnabledAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.KpiThresholds
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.MetricKey)
            .ToListAsync(cancellationToken);
    }

    public Task CreateAsync(KpiThresholdDefinition threshold, CancellationToken cancellationToken)
    {
        _dbContext.KpiThresholds.Add(threshold);
        return Task.CompletedTask;
    }
}
