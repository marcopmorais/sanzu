namespace Sanzu.Core.Models.Responses;

public sealed class RemediationActionResponse
{
    public Guid Id { get; init; }
    public string QueueId { get; init; } = string.Empty;
    public string QueueItemId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string AuditNote { get; init; } = string.Empty;
    public string? ImpactSummary { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? VerificationType { get; init; }
    public string? VerificationResult { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? CommittedAt { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public sealed class RemediationImpactPreviewResponse
{
    public string ActionType { get; init; } = string.Empty;
    public string ImpactSummary { get; init; } = string.Empty;
    public bool IsReversible { get; init; }
    public IReadOnlyList<string> AffectedEntities { get; init; } = [];
}
