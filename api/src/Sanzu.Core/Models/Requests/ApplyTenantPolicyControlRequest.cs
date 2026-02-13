namespace Sanzu.Core.Models.Requests;

public sealed class ApplyTenantPolicyControlRequest
{
    public string ControlType { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}
