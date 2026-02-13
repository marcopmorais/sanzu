using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantLifecycleService
{
    Task<TenantLifecycleStateResponse> UpdateTenantLifecycleStateAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantLifecycleStateRequest request,
        CancellationToken cancellationToken);
}
