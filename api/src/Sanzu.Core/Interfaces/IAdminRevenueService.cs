using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface IAdminRevenueService
{
    Task<RevenueOverviewResponse> GetRevenueOverviewAsync(CancellationToken cancellationToken);
    Task<RevenueTrendsResponse> GetRevenueTrendsAsync(string period, CancellationToken cancellationToken);
    Task<BillingHealthResponse> GetBillingHealthAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<RevenueExportRow>> GetRevenueExportDataAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BillingHealthExportRow>> GetBillingHealthExportDataAsync(CancellationToken cancellationToken);
}
