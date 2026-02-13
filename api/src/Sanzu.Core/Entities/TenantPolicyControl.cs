using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class TenantPolicyControl
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public TenantPolicyControlType ControlType { get; set; }
    public bool IsEnabled { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public Guid AppliedByUserId { get; set; }
    public DateTime AppliedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
