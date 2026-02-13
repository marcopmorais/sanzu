using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantLifecycleStateResponse
{
    public Guid TenantId { get; init; }
    public TenantStatus PreviousStatus { get; init; }
    public TenantStatus CurrentStatus { get; init; }
    public string Reason { get; init; } = string.Empty;
    public Guid ChangedByUserId { get; init; }
    public DateTime ChangedAt { get; init; }
}
