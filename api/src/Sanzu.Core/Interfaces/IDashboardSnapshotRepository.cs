using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IDashboardSnapshotRepository
{
    Task<DashboardSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshot> CreateOrUpdateAsync(DashboardSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
}
