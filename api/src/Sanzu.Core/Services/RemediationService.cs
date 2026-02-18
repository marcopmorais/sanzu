using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class RemediationService : IRemediationService
{
    private readonly IRemediationRepository _remediationRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Dictionary<string, (string Summary, bool Reversible)> ActionCatalog = new()
    {
        ["contact_tenant"] = ("Contact tenant to resolve issue.", true),
        ["extend_grace_period"] = ("Extend payment grace period for tenant.", true),
        ["reset_onboarding"] = ("Reset onboarding state to allow retry.", true),
        ["apply_policy_override"] = ("Apply temporary policy override.", true),
        ["suspend_tenant"] = ("Suspend tenant access.", false),
        ["escalate_to_support"] = ("Escalate to support engineering.", true),
        ["run_diagnostics"] = ("Run least-privilege diagnostics.", true)
    };

    public RemediationService(
        IRemediationRepository remediationRepository,
        IAuditRepository auditRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork)
    {
        _remediationRepository = remediationRepository;
        _auditRepository = auditRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RemediationImpactPreviewResponse> PreviewAsync(
        Guid actorUserId,
        string actionType,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        if (!ActionCatalog.TryGetValue(actionType, out var catalog))
        {
            throw new ValidationException($"Unknown action type: {actionType}");
        }

        return new RemediationImpactPreviewResponse
        {
            ActionType = actionType,
            ImpactSummary = catalog.Summary,
            IsReversible = catalog.Reversible,
            AffectedEntities = [$"Tenant:{tenantId}"]
        };
    }

    public async Task<RemediationActionResponse> CommitAsync(
        Guid actorUserId,
        CommitRemediationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.AuditNote))
        {
            throw new ValidationException("Audit note is required for remediation actions.");
        }

        if (!ActionCatalog.TryGetValue(request.ActionType, out var catalog))
        {
            throw new ValidationException($"Unknown action type: {request.ActionType}");
        }

        var now = DateTime.UtcNow;
        var action = new RemediationAction
        {
            Id = Guid.NewGuid(),
            QueueId = request.QueueId,
            QueueItemId = request.QueueItemId,
            TenantId = request.TenantId,
            ActionType = request.ActionType,
            AuditNote = request.AuditNote,
            ImpactSummary = catalog.Summary,
            Status = RemediationStatus.Committed,
            CommittedByUserId = actorUserId,
            CreatedAt = now,
            CommittedAt = now
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _remediationRepository.CreateAsync(action, ct);
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "RemediationCommitted",
                Metadata = $"{{\"remediationId\":\"{action.Id}\",\"actionType\":\"{request.ActionType}\",\"tenantId\":\"{request.TenantId}\"}}",
                CreatedAt = now
            }, ct);
        }, cancellationToken);

        return MapToResponse(action);
    }

    public async Task<RemediationActionResponse> VerifyAsync(
        Guid actorUserId,
        Guid remediationId,
        VerifyRemediationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var action = await _remediationRepository.GetByIdAsync(remediationId, cancellationToken);
        if (action == null)
        {
            throw new CaseStateException("Remediation action not found.");
        }

        if (action.Status != RemediationStatus.Committed && action.Status != RemediationStatus.VerificationStarted)
        {
            throw new CaseStateException($"Cannot verify a remediation in status {action.Status}.");
        }

        var now = DateTime.UtcNow;
        action.VerificationType = request.VerificationType;
        action.VerificationResult = request.VerificationResult;
        action.VerifiedByUserId = actorUserId;
        action.VerifiedAt = now;
        action.Status = request.Passed ? RemediationStatus.Verified : RemediationStatus.VerificationFailed;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = request.Passed ? "RemediationVerified" : "RemediationVerificationFailed",
                Metadata = $"{{\"remediationId\":\"{remediationId}\",\"verificationType\":\"{request.VerificationType}\"}}",
                CreatedAt = now
            }, ct);
        }, cancellationToken);

        return MapToResponse(action);
    }

    public async Task<RemediationActionResponse> ResolveAsync(
        Guid actorUserId,
        Guid remediationId,
        ResolveRemediationRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var action = await _remediationRepository.GetByIdAsync(remediationId, cancellationToken);
        if (action == null)
        {
            throw new CaseStateException("Remediation action not found.");
        }

        if (action.Status == RemediationStatus.Verified)
        {
            // Normal resolve after verification passed
        }
        else if (action.Status == RemediationStatus.VerificationFailed && !string.IsNullOrWhiteSpace(request.OverrideNote))
        {
            // Override resolve with audit note
            action.AuditNote += $" | Override: {request.OverrideNote}";
        }
        else
        {
            throw new CaseStateException("Cannot resolve: verification must pass, or an override note is required.");
        }

        var now = DateTime.UtcNow;
        action.Status = RemediationStatus.Resolved;
        action.ResolvedAt = now;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = "RemediationResolved",
                Metadata = $"{{\"remediationId\":\"{remediationId}\"}}",
                CreatedAt = now
            }, ct);
        }, cancellationToken);

        return MapToResponse(action);
    }

    public async Task<RemediationActionResponse> GetByIdAsync(
        Guid actorUserId,
        Guid remediationId,
        CancellationToken cancellationToken)
    {
        await EnsureSanzuAdminAccessAsync(actorUserId, cancellationToken);

        var action = await _remediationRepository.GetByIdAsync(remediationId, cancellationToken);
        if (action == null)
        {
            throw new CaseStateException("Remediation action not found.");
        }

        return MapToResponse(action);
    }

    private static RemediationActionResponse MapToResponse(RemediationAction action) => new()
    {
        Id = action.Id,
        QueueId = action.QueueId,
        QueueItemId = action.QueueItemId,
        TenantId = action.TenantId,
        ActionType = action.ActionType,
        AuditNote = action.AuditNote,
        ImpactSummary = action.ImpactSummary,
        Status = action.Status.ToString(),
        VerificationType = action.VerificationType,
        VerificationResult = action.VerificationResult,
        CreatedAt = action.CreatedAt,
        CommittedAt = action.CommittedAt,
        VerifiedAt = action.VerifiedAt,
        ResolvedAt = action.ResolvedAt
    };

    private async Task EnsureSanzuAdminAccessAsync(Guid actorUserId, CancellationToken cancellationToken)
    {
        var actorRoles = await _userRoleRepository.GetByUserIdAsync(actorUserId, cancellationToken);
        var hasPlatformRole = actorRoles.Any(
            role => role.RoleType == PlatformRole.SanzuAdmin
                    && (role.TenantId == null || role.TenantId == Guid.Empty));

        if (!hasPlatformRole)
        {
            throw new TenantAccessDeniedException();
        }
    }
}
