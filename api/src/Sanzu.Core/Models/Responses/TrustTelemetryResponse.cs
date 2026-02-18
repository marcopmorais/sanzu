namespace Sanzu.Core.Models.Responses;

public sealed class TrustTelemetryResponse
{
    public Guid? TenantId { get; init; }
    public int PeriodDays { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime GeneratedAt { get; init; }
    public TrustTelemetryMetricsResponse Metrics { get; init; } = new();
    public IReadOnlyList<ReasonCodeCountResponse> BlockedByReason { get; init; } = [];
    public IReadOnlyList<TrustTelemetryEventSummaryResponse> EventSummary { get; init; } = [];
}

public sealed class TrustTelemetryMetricsResponse
{
    public int CasesCreated { get; init; }
    public int CasesClosed { get; init; }
    public int TasksBlocked { get; init; }
    public int TasksCompleted { get; init; }
    public int PlaybooksApplied { get; init; }
    public int DocumentsUploaded { get; init; }
}

public sealed class ReasonCodeCountResponse
{
    public string ReasonCategory { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class TrustTelemetryEventSummaryResponse
{
    public string EventType { get; init; } = string.Empty;
    public int Count { get; init; }
}
