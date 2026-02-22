namespace Sanzu.Core.Models.Responses;

public sealed class TenantBillingResponse
{
    public string? SubscriptionPlan { get; init; }
    public string? BillingCycle { get; init; }
    public DateTime? SubscriptionActivatedAt { get; init; }
    public string BillingHealth { get; init; } = "Paid";
    public DateTime? LastPaymentDate { get; init; }
    public DateTime? NextRenewalDate { get; init; }
    public bool GracePeriodActive { get; init; }
    public DateTime? GracePeriodRetryAt { get; init; }
    public IReadOnlyList<TenantBillingInvoiceItem> RecentInvoices { get; init; } = [];
}

public sealed record TenantBillingInvoiceItem(
    string InvoiceNumber,
    DateTime BillingCycleStart,
    DateTime BillingCycleEnd,
    decimal TotalAmount,
    string Currency,
    string Status,
    DateTime CreatedAt
);
