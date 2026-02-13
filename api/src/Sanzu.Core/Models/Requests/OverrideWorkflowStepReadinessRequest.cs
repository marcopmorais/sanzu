namespace Sanzu.Core.Models.Requests;

public sealed class OverrideWorkflowStepReadinessRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
}
