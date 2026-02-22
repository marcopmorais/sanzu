using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;

namespace Sanzu.Core.Services;

public sealed class SupportActionsService : ISupportActionsService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SupportActionsService(
        IOrganizationRepository organizationRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository = organizationRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task OverrideBlockedStepAsync(
        Guid tenantId, Guid caseId, Guid stepId, string rationale, Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken)
                     ?? throw new InvalidOperationException("Tenant not found");

        var caseEntity = await _caseRepository.GetByIdForPlatformAsync(caseId, cancellationToken)
                         ?? throw new InvalidOperationException("Case not found");

        if (caseEntity.TenantId != tenantId)
            throw new InvalidOperationException("Case does not belong to specified tenant");

        var step = await _workflowStepRepository.GetByIdAsync(stepId, cancellationToken)
                   ?? throw new InvalidOperationException("Workflow step not found");

        if (step.CaseId != caseId)
            throw new InvalidOperationException("Step does not belong to specified case");

        var previousStatus = step.Status.ToString();

        step.Status = WorkflowStepStatus.Ready;
        step.BlockedReasonCode = null;
        step.BlockedReasonDetail = null;
        step.IsReadinessOverridden = true;
        step.ReadinessOverrideRationale = rationale;
        step.ReadinessOverrideByUserId = actorUserId;
        step.ReadinessOverriddenAt = DateTime.UtcNow;
        step.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                CaseId = caseId,
                EventType = "Admin.Tenant.WorkflowStepOverridden",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenantId,
                    caseId,
                    stepId,
                    stepKey = step.StepKey,
                    previousStatus,
                    newStatus = "Ready",
                    rationale
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }

    public async Task ExtendGracePeriodAsync(
        Guid tenantId, int days, string rationale, Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken)
                     ?? throw new InvalidOperationException("Tenant not found");

        var previousRetryAt = tenant.NextPaymentRetryAt;
        tenant.NextPaymentRetryAt = (tenant.NextPaymentRetryAt ?? DateTime.UtcNow).AddDays(days);
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Tenant.GracePeriodExtended",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenantId,
                    days,
                    rationale,
                    previousRetryAt,
                    newRetryAt = tenant.NextPaymentRetryAt,
                    subscriptionPlan = tenant.SubscriptionPlan,
                    failedPaymentAttempts = tenant.FailedPaymentAttempts
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }

    public async Task TriggerReOnboardingAsync(
        Guid tenantId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken)
                     ?? throw new InvalidOperationException("Tenant not found");

        var previousStatus = tenant.Status.ToString();
        tenant.Status = TenantStatus.Onboarding;
        tenant.OnboardingCompletedAt = null;
        tenant.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Tenant.ReOnboardingTriggered",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenantId,
                    previousStatus,
                    newStatus = "Onboarding"
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }

    public async Task<ImpersonationResult> StartImpersonationAsync(
        Guid tenantId, Guid actorUserId, CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdForPlatformAsync(tenantId, cancellationToken)
                     ?? throw new InvalidOperationException("Tenant not found");

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    .Replace("+", "-").Replace("/", "_").TrimEnd('=');
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "Admin.Tenant.ImpersonationStarted",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    tenantId,
                    tenantName = tenant.Name,
                    expiresAt
                }),
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        return new ImpersonationResult
        {
            Token = token,
            ExpiresAt = expiresAt,
            TenantId = tenantId,
            TenantName = tenant.Name
        };
    }
}
