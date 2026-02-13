using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class KpiAlertLog
{
    public Guid Id { get; set; }
    public Guid ThresholdId { get; set; }
    public KpiMetricKey MetricKey { get; set; }
    public int ThresholdValue { get; set; }
    public int ActualValue { get; set; }
    public KpiAlertSeverity Severity { get; set; }
    public string RouteTarget { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public Guid TriggeredByUserId { get; set; }
    public DateTime TriggeredAt { get; set; }
}
