namespace Sanzu.Core.Models.Requests;

public sealed class UpdateTenantLifecycleStateRequest
{
    public string TargetStatus { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
