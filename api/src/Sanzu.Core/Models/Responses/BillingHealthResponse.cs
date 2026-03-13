namespace Sanzu.Core.Models.Responses;

public sealed class BillingHealthResponse
{
    public int FailedPaymentCount { get; init; }
    public int OverdueInvoiceCount { get; init; }
    public int GracePeriodCount { get; init; }
    public IReadOnlyList<BillingHealthTenantItem> FailedPayments { get; init; } = [];
    public IReadOnlyList<BillingHealthTenantItem> GracePeriodTenants { get; init; } = [];
    public IReadOnlyList<BillingHealthTenantItem> UpcomingRenewals { get; init; } = [];
}

public sealed class BillingHealthTenantItem
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public decimal? FailedAmount { get; init; }
    public DateTime? LastFailedAt { get; init; }
    public DateTime? GracePeriodRetryAt { get; init; }
    public DateTime? NextRenewalDate { get; init; }
}
