using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ITenantPolicyControlService
{
    Task<TenantPolicyControlResponse> ApplyTenantPolicyControlAsync(
        Guid tenantId,
        Guid actorUserId,
        ApplyTenantPolicyControlRequest request,
        CancellationToken cancellationToken);
}
