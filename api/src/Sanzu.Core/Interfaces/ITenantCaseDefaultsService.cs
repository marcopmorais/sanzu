using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantCaseDefaultsService
{
    Task<TenantCaseDefaultsResponse> GetCaseDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken);

    Task<TenantCaseDefaultsResponse> UpdateCaseDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantCaseDefaultsRequest request,
        CancellationToken cancellationToken);
}
