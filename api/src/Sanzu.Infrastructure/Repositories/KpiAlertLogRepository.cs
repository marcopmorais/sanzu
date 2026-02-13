using Sanzu.Core.Entities;
using Sanzu.Core.Interfaces;
using Sanzu.Infrastructure.Data;

namespace Sanzu.Infrastructure.Repositories;

public sealed class KpiAlertLogRepository : IKpiAlertLogRepository
{
    private readonly SanzuDbContext _dbContext;

    public KpiAlertLogRepository(SanzuDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task CreateAsync(KpiAlertLog alertLog, CancellationToken cancellationToken)
    {
        _dbContext.KpiAlerts.Add(alertLog);
        return Task.CompletedTask;
    }
}
