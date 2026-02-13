using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Core.Interfaces;

public interface IKpiThresholdRepository
{
    Task<KpiThresholdDefinition?> GetByMetricAsync(KpiMetricKey metricKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<KpiThresholdDefinition>> GetEnabledAsync(CancellationToken cancellationToken);
    Task CreateAsync(KpiThresholdDefinition threshold, CancellationToken cancellationToken);
}
