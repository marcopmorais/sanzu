using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IKpiAlertLogRepository
{
    Task CreateAsync(KpiAlertLog alertLog, CancellationToken cancellationToken);
}
