using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantUsageIndicatorsService
{
    Task<TenantUsageIndicatorsResponse> GetUsageIndicatorsAsync(
        Guid tenantId,
        Guid actorUserId,
        int periodDays,
        CancellationToken cancellationToken);
}
