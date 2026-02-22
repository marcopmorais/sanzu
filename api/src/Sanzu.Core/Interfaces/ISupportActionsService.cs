namespace Sanzu.Core.Interfaces;

public interface ISupportActionsService
{
    Task OverrideBlockedStepAsync(Guid tenantId, Guid caseId, Guid stepId, string rationale, Guid actorUserId, CancellationToken cancellationToken);
    Task ExtendGracePeriodAsync(Guid tenantId, int days, string rationale, Guid actorUserId, CancellationToken cancellationToken);
    Task TriggerReOnboardingAsync(Guid tenantId, Guid actorUserId, CancellationToken cancellationToken);
    Task<ImpersonationResult> StartImpersonationAsync(Guid tenantId, Guid actorUserId, CancellationToken cancellationToken);
}

public sealed class ImpersonationResult
{
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
}
