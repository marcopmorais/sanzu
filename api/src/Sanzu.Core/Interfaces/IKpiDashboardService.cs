using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IKpiDashboardService
{
    Task<PlatformKpiDashboardResponse> GetDashboardAsync(
        Guid actorUserId,
        int periodDays,
        int tenantLimit,
        int caseLimit,
        CancellationToken cancellationToken);
}
