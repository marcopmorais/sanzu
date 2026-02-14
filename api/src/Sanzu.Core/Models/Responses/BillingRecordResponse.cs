namespace Sanzu.Core.Models.Responses;

public sealed class BillingRecordResponse
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public DateTime BillingCycleStart { get; init; }
    public DateTime BillingCycleEnd { get; init; }
    public string PlanCode { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public decimal BaseAmount { get; init; }
    public int OverageUnits { get; init; }
    public decimal OverageAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
