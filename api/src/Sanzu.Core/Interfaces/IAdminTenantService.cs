using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminTenantService
{
    Task<PaginatedResponse<TenantListItemResponse>> ListTenantsAsync(
        TenantListRequest request, CancellationToken cancellationToken);
}
