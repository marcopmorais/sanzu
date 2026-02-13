using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantLifecycleService : ITenantLifecycleService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateTenantLifecycleStateRequest> _updateTenantLifecycleStateValidator;

    public TenantLifecycleService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateTenantLifecycleStateRequest> updateTenantLifecycleStateValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _updateTenantLifecycleStateValidator = updateTenantLifecycleStateValidator;
    }

    public async Task<TenantLifecycleStateResponse> UpdateTenantLifecycleStateAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantLifecycleStateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateTenantLifecycleStateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedTargetStatus = ParseTargetStatus(request.TargetStatus);
        var normalizedReason = request.Reason.Trim();

        TenantLifecycleStateResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await _organizationRepository.GetByIdAsync(tenantId, token);
                if (tenant is null)
                {
                    throw new TenantAccessDeniedException();
                }

                await EnsureSanzuAdminAccessAsync(actorUserId, token);
                EnsureValidTransition(tenant.Status, normalizedTargetStatus);

                var nowUtc = DateTime.UtcNow;
                var previousStatus = tenant.Status;
                tenant.Status = normalizedTargetStatus;
                tenant.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantLifecycleStateChanged",
                    new
                    {
                        TenantId = tenant.Id,
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = tenant.Status.ToString(),
                        Reason = normalizedReason,
                        ChangedAt = nowUtc
                    },
                    token);

                response = new TenantLifecycleStateResponse
                {
                    TenantId = tenant.Id,
                    PreviousStatus = previousStatus,
                    CurrentStatus = tenant.Status,
                    Reason = normalizedReason,
                    ChangedByUserId = actorUserId,
                    ChangedAt = nowUtc
                };
            },
            cancellationToken);

        return response!;
    }

    private static TenantStatus ParseTargetStatus(string targetStatus)
    {
        if (!Enum.TryParse<TenantStatus>(targetStatus, ignoreCase: true, out var parsedStatus))
        {
            throw new TenantLifecycleStateException("Target status is not valid.");
        }

        return parsedStatus;
    }

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

    private static void EnsureValidTransition(TenantStatus currentStatus, TenantStatus targetStatus)
    {
        if (currentStatus == targetStatus)
        {
            throw new TenantLifecycleStateException(
                $"Tenant is already in {targetStatus} state.");
        }

        var isAllowed = currentStatus switch
        {
            TenantStatus.Pending => targetStatus is TenantStatus.Onboarding
                or TenantStatus.Active
                or TenantStatus.Suspended
                or TenantStatus.Terminated,
            TenantStatus.Onboarding => targetStatus is TenantStatus.Active
                or TenantStatus.Suspended
                or TenantStatus.Terminated,
            TenantStatus.Active => targetStatus is TenantStatus.PaymentIssue
                or TenantStatus.Suspended
                or TenantStatus.Terminated,
            TenantStatus.PaymentIssue => targetStatus is TenantStatus.Active
                or TenantStatus.Suspended
                or TenantStatus.Terminated,
            TenantStatus.Suspended => targetStatus is TenantStatus.Active
                or TenantStatus.Terminated,
            TenantStatus.Terminated => false,
            _ => false
        };

        if (!isAllowed)
        {
            throw new TenantLifecycleStateException(
                $"Invalid tenant lifecycle transition from {currentStatus} to {targetStatus}.");
        }
    }

    private Task WriteAuditEventAsync(
        Guid actorUserId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EventType = eventType,
            Metadata = JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}
