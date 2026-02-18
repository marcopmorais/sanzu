namespace Sanzu.Core.Models.Requests;

public sealed class CommitRemediationRequest
{
    public string QueueId { get; init; } = string.Empty;
    public string QueueItemId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public string AuditNote { get; init; } = string.Empty;
}

public sealed class VerifyRemediationRequest
{
    public string VerificationType { get; init; } = string.Empty;
    public string? VerificationResult { get; init; }
    public bool Passed { get; init; }
}

public sealed class ResolveRemediationRequest
{
    public string? OverrideNote { get; init; }
}
