namespace Sanzu.Core.Entities;

public sealed class BillingRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime BillingCycleStart { get; set; }
    public DateTime BillingCycleEnd { get; set; }
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal BaseAmount { get; set; }
    public int OverageUnits { get; set; }
    public decimal OverageAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "FINALIZED";
    public string InvoiceSnapshot { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Organization? Tenant { get; set; }
}
