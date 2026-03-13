namespace Sanzu.Core.Models.Responses;

public sealed class RevenueOverviewResponse
{
    public decimal Mrr { get; init; }
    public decimal Arr { get; init; }
    public decimal ChurnRate { get; init; }
    public decimal GrowthRate { get; init; }
    public IReadOnlyList<PlanRevenueItem> PlanBreakdown { get; init; } = [];
}

public sealed record PlanRevenueItem(
    string PlanName,
    int TenantCount,
    decimal Mrr,
    decimal Percentage
);
