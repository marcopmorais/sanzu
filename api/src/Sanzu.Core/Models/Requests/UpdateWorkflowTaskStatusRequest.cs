namespace Sanzu.Core.Models.Requests;

public sealed class UpdateWorkflowTaskStatusRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
