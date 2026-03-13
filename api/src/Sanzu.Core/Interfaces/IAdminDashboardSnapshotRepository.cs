using Sanzu.Core.Entities;

namespace Sanzu.Core.Interfaces;

public interface IAdminDashboardSnapshotRepository
{
    Task<AdminDashboardSnapshot?> GetLatestByTypeAsync(string snapshotType, CancellationToken cancellationToken);
    Task UpsertAsync(AdminDashboardSnapshot snapshot, CancellationToken cancellationToken);
}
