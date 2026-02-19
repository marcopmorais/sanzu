using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface ITenantHealthScoreRepository
{
    Task<TenantHealthScore?> GetLatestByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantHealthScore>> GetLatestForAllTenantsAsync(CancellationToken cancellationToken);
    Task CreateAsync(TenantHealthScore score, CancellationToken cancellationToken);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken);
}
