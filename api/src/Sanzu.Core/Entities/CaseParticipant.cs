using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class CaseParticipant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string Email { get; set; } = string.Empty;
    public CaseRole Role { get; set; }
    public CaseParticipantStatus Status { get; set; } = CaseParticipantStatus.Pending;
    public string TokenHash { get; set; } = string.Empty;
    public Guid InvitedByUserId { get; set; }
    public Guid? ParticipantUserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }

    public Organization? Tenant { get; set; }
    public Case? Case { get; set; }
    public User? InvitedByUser { get; set; }
    public User? ParticipantUser { get; set; }
}
