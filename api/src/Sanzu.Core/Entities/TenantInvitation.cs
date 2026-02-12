using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class TenantInvitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public PlatformRole RoleType { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public TenantInvitationStatus Status { get; set; } = TenantInvitationStatus.Pending;
    public Guid InvitedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public Organization? Tenant { get; set; }
    public User? InvitedByUser { get; set; }
}
