using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminTenantService
{
    Task<PaginatedResponse<TenantListItemResponse>> ListTenantsAsync(
        TenantListRequest request, CancellationToken cancellationToken);

    Task<TenantSummaryResponse?> GetTenantSummaryAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantBillingResponse?> GetTenantBillingAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantCasesResponse?> GetTenantCasesAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<TenantActivityResponse?> GetTenantActivityAsync(Guid tenantId, CancellationToken cancellationToken);
}
