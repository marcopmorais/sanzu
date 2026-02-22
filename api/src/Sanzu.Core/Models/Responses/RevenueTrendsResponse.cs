namespace Sanzu.Core.Models.Responses;

public sealed class RevenueTrendsResponse
{
    public IReadOnlyList<RevenueTrendPoint> DataPoints { get; init; } = [];
}

public sealed record RevenueTrendPoint(
    string PeriodLabel,
    decimal Mrr,
    int TenantCount
);
