namespace Sanzu.Core.Models.Requests;

public sealed class ActivateTenantBillingRequest
{
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public string PaymentMethodType { get; set; } = string.Empty;
    public string PaymentMethodReference { get; set; } = string.Empty;
    public string InvoiceProfileLegalName { get; set; } = string.Empty;
    public string? InvoiceProfileVatNumber { get; set; }
    public string InvoiceProfileBillingEmail { get; set; } = string.Empty;
    public string InvoiceProfileCountryCode { get; set; } = string.Empty;
}
