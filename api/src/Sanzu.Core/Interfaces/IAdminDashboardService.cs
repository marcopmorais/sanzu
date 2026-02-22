using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminDashboardService
{
    Task ComputeSnapshotAsync(CancellationToken cancellationToken);
    Task<AdminDashboardSummary?> GetLatestSnapshotAsync(CancellationToken cancellationToken);
}
