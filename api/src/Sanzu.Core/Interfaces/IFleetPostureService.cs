using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IFleetPostureService
{
    Task<FleetPostureResponse> GetFleetPostureAsync(
        Guid actorUserId,
        string? search,
        string? statusFilter,
        CancellationToken cancellationToken);

    Task<TenantDrilldownResponse> GetTenantDrilldownAsync(
        Guid actorUserId,
        Guid tenantId,
        CancellationToken cancellationToken);
}
