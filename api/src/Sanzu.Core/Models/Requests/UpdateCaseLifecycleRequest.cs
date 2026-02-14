namespace Sanzu.Core.Models.Requests;

public sealed class UpdateCaseLifecycleRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string? Reason { get; init; }
}
