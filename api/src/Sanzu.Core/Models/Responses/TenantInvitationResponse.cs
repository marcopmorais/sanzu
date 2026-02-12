using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantInvitationResponse
{
    public Guid InvitationId { get; init; }
    public Guid TenantId { get; init; }
    public string Email { get; init; } = string.Empty;
    public PlatformRole RoleType { get; init; }
    public DateTime ExpiresAt { get; init; }
    public TenantInvitationStatus Status { get; init; }
}
