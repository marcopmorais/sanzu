using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantPolicyControlResponse
{
    public Guid TenantId { get; init; }
    public TenantPolicyControlType ControlType { get; init; }
    public bool IsEnabled { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
    public Guid AppliedByUserId { get; init; }
    public DateTime AppliedAt { get; init; }
}
