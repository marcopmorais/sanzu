namespace Sanzu.Core.Models.Requests;

public sealed class UpsertKpiThresholdRequest
{
    public string MetricKey { get; init; } = string.Empty;
    public int ThresholdValue { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string RouteTarget { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
}
