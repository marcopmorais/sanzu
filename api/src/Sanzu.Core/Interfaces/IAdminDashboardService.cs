using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminDashboardService
{
    Task ComputeSnapshotAsync(CancellationToken cancellationToken);
    Task<AdminDashboardSummary?> GetLatestSnapshotAsync(CancellationToken cancellationToken);
    Task<DashboardResponse<AdminDashboardSummary>> GetDashboardAsync(int snapshotIntervalMinutes, CancellationToken cancellationToken);
}
