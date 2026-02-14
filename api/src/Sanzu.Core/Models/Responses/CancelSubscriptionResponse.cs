using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CancelSubscriptionResponse
{
    public Guid TenantId { get; init; }
    public TenantStatus TenantStatus { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
