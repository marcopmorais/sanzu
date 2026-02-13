using Sanzu.Core.Entities;
using Sanzu.Core.Enums;

namespace Sanzu.Core.Interfaces;

public interface ITenantPolicyControlRepository
{
    Task<TenantPolicyControl?> GetByTenantAndControlAsync(
        Guid tenantId,
        TenantPolicyControlType controlType,
        CancellationToken cancellationToken);

    Task<bool> IsControlEnabledAsync(
        Guid tenantId,
        TenantPolicyControlType controlType,
        CancellationToken cancellationToken);

    Task CreateAsync(TenantPolicyControl control, CancellationToken cancellationToken);
}
