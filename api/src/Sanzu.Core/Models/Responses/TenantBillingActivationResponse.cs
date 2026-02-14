using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantBillingActivationResponse
{
    public Guid TenantId { get; init; }
    public TenantStatus TenantStatus { get; init; }
    public string PlanCode { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public string PaymentMethodType { get; init; } = string.Empty;
    public string InvoiceProfileBillingEmail { get; init; } = string.Empty;
    public DateTime SubscriptionActivatedAt { get; init; }
}
