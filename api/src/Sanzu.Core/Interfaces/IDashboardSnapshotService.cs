using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IDashboardSnapshotService
{
    Task<DashboardSnapshot> ComputeSnapshotAsync(CancellationToken cancellationToken = default);
    Task<DashboardSnapshot?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<bool> IsStaleAsync(CancellationToken cancellationToken = default);
}
