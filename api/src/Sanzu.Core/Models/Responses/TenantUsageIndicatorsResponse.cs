namespace Sanzu.Core.Models.Responses;

public sealed class TenantUsageIndicatorsResponse
{
    public Guid TenantId { get; init; }
    public int PeriodDays { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public TenantUsageCurrentMetricsResponse Current { get; init; } = new();
    public IReadOnlyList<TenantUsageHistoryPointResponse> History { get; init; } =
        Array.Empty<TenantUsageHistoryPointResponse>();
}

public sealed class TenantUsageCurrentMetricsResponse
{
    public int CasesCreated { get; init; }
    public int CasesClosed { get; init; }
    public int ActiveCases { get; init; }
    public int DocumentsUploaded { get; init; }
}

public sealed class TenantUsageHistoryPointResponse
{
    public DateTime Date { get; init; }
    public int CasesCreated { get; init; }
    public int CasesClosed { get; init; }
    public int DocumentsUploaded { get; init; }
}
