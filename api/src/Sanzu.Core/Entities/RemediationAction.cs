using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class RemediationAction
{
    public Guid Id { get; set; }
    public string QueueId { get; set; } = string.Empty;
    public string QueueItemId { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string AuditNote { get; set; } = string.Empty;
    public string? ImpactSummary { get; set; }
    public RemediationStatus Status { get; set; } = RemediationStatus.Pending;
    public string? VerificationType { get; set; }
    public string? VerificationResult { get; set; }
    public Guid CommittedByUserId { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CommittedAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
