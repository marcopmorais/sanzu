using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CaseParticipantResponse
{
    public Guid ParticipantId { get; init; }
    public Guid TenantId { get; init; }
    public Guid CaseId { get; init; }
    public string Email { get; init; } = string.Empty;
    public CaseRole Role { get; init; }
    public CaseParticipantStatus Status { get; init; }
    public Guid InvitedByUserId { get; init; }
    public Guid? ParticipantUserId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? AcceptedAt { get; init; }
}
