using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantPolicyControlService : ITenantPolicyControlService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ITenantPolicyControlRepository _tenantPolicyControlRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<ApplyTenantPolicyControlRequest> _applyTenantPolicyControlValidator;

    public TenantPolicyControlService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        ITenantPolicyControlRepository tenantPolicyControlRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<ApplyTenantPolicyControlRequest> applyTenantPolicyControlValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _tenantPolicyControlRepository = tenantPolicyControlRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _applyTenantPolicyControlValidator = applyTenantPolicyControlValidator;
    }

    public async Task<TenantPolicyControlResponse> ApplyTenantPolicyControlAsync(
        Guid tenantId,
        Guid actorUserId,
        ApplyTenantPolicyControlRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _applyTenantPolicyControlValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var controlType = ParseControlType(request.ControlType);
        var reasonCode = request.ReasonCode.Trim().ToUpperInvariant();
        TenantPolicyControlResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await _organizationRepository.GetByIdAsync(tenantId, token);
                if (tenant is null)
                {
                    throw new TenantAccessDeniedException();
                }

                await EnsureSanzuAdminAccessAsync(actorUserId, token);

                var nowUtc = DateTime.UtcNow;
                var control = await _tenantPolicyControlRepository.GetByTenantAndControlAsync(
                    tenantId,
                    controlType,
                    token);

                if (control is null)
                {
                    control = new TenantPolicyControl
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ControlType = controlType,
                        IsEnabled = request.IsEnabled,
                        ReasonCode = reasonCode,
                        AppliedByUserId = actorUserId,
                        AppliedAt = nowUtc,
                        UpdatedAt = nowUtc
                    };

                    await _tenantPolicyControlRepository.CreateAsync(control, token);
                }
                else
                {
                    control.IsEnabled = request.IsEnabled;
                    control.ReasonCode = reasonCode;
                    control.AppliedByUserId = actorUserId;
                    control.AppliedAt = nowUtc;
                    control.UpdatedAt = nowUtc;
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantPolicyControlApplied",
                    new
                    {
                        TenantId = tenantId,
                        ControlType = controlType.ToString(),
                        IsEnabled = request.IsEnabled,
                        ReasonCode = reasonCode,
                        AppliedAt = nowUtc
                    },
                    token);

                response = new TenantPolicyControlResponse
                {
                    TenantId = tenantId,
                    ControlType = controlType,
                    IsEnabled = request.IsEnabled,
                    ReasonCode = reasonCode,
                    AppliedByUserId = actorUserId,
                    AppliedAt = nowUtc
                };
            },
            cancellationToken);

        return response!;
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

    private static TenantPolicyControlType ParseControlType(string value)
    {
        if (!Enum.TryParse<TenantPolicyControlType>(value, ignoreCase: true, out var controlType))
        {
            throw new ValidationException("ControlType must be a valid tenant policy control.");
        }

        return controlType;
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
