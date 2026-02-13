using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class KpiThresholdDefinition
{
    public Guid Id { get; set; }
    public KpiMetricKey MetricKey { get; set; }
    public int ThresholdValue { get; set; }
    public KpiAlertSeverity Severity { get; set; }
    public string RouteTarget { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Guid UpdatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
}
