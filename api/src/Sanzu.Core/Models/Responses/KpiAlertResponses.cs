using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class KpiThresholdResponse
{
    public Guid ThresholdId { get; init; }
    public KpiMetricKey MetricKey { get; init; }
    public int ThresholdValue { get; init; }
    public KpiAlertSeverity Severity { get; init; }
    public string RouteTarget { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public Guid UpdatedByUserId { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class KpiAlertEvaluationResponse
{
    public int PeriodDays { get; init; }
    public DateTime EvaluatedAt { get; init; }
    public IReadOnlyList<KpiAlertLogResponse> GeneratedAlerts { get; init; } = Array.Empty<KpiAlertLogResponse>();
}

public sealed class KpiAlertLogResponse
{
    public Guid AlertId { get; init; }
    public Guid ThresholdId { get; init; }
    public KpiMetricKey MetricKey { get; init; }
    public int ThresholdValue { get; init; }
    public int ActualValue { get; init; }
    public KpiAlertSeverity Severity { get; init; }
    public string RouteTarget { get; init; } = string.Empty;
    public string Context { get; init; } = string.Empty;
    public DateTime TriggeredAt { get; init; }
}
