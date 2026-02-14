namespace Sanzu.Core.Models.Responses;

public sealed class BillingUsageSummaryResponse
{
    public Guid TenantId { get; init; }
    public string PlanCode { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public decimal MonthlyPrice { get; init; }
    public int IncludedCases { get; init; }
    public int UsedCases { get; init; }
    public int OverageCases { get; init; }
    public decimal OverageUnitPrice { get; init; }
    public DateTime CurrentPeriodStart { get; init; }
    public DateTime CurrentPeriodEnd { get; init; }
    public DateTime SubscriptionActivatedAt { get; init; }
}
