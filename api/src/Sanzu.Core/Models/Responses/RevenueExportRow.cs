namespace Sanzu.Core.Models.Responses;

public sealed class RevenueExportRow
{
    public string TenantName { get; init; } = string.Empty;
    public string PlanTier { get; init; } = string.Empty;
    public decimal MrrContribution { get; init; }
    public string BillingStatus { get; init; } = string.Empty;
    public DateTime? LastPaymentDate { get; init; }
    public DateTime? NextRenewal { get; init; }
}

public sealed class BillingHealthExportRow
{
    public string TenantName { get; init; } = string.Empty;
    public string IssueType { get; init; } = string.Empty;
    public decimal? FailedAmount { get; init; }
    public DateTime? LastFailedAt { get; init; }
    public DateTime? GracePeriodRetryAt { get; init; }
    public DateTime? NextRenewalDate { get; init; }
}
