using System.Text.Json;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantCaseDefaultsService : ITenantCaseDefaultsService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<UpdateTenantCaseDefaultsRequest> _updateCaseDefaultsValidator;

    public TenantCaseDefaultsService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<UpdateTenantCaseDefaultsRequest> updateCaseDefaultsValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _updateCaseDefaultsValidator = updateCaseDefaultsValidator;
    }

    public async Task<TenantCaseDefaultsResponse> GetCaseDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, cancellationToken);
        return MapCaseDefaults(tenant);
    }

    public async Task<TenantCaseDefaultsResponse> UpdateCaseDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantCaseDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateCaseDefaultsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantCaseDefaultsResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureCaseDefaultsMutableState(tenant);

                var previousWorkflowKey = tenant.DefaultWorkflowKey;
                var previousTemplateKey = tenant.DefaultTemplateKey;

                if (request.DefaultWorkflowKey is not null)
                {
                    tenant.DefaultWorkflowKey = request.DefaultWorkflowKey.Trim();
                }

                if (request.DefaultTemplateKey is not null)
                {
                    tenant.DefaultTemplateKey = request.DefaultTemplateKey.Trim();
                }

                var defaultsChanged =
                    !string.Equals(previousWorkflowKey, tenant.DefaultWorkflowKey, StringComparison.Ordinal)
                    || !string.Equals(previousTemplateKey, tenant.DefaultTemplateKey, StringComparison.Ordinal);

                if (defaultsChanged)
                {
                    tenant.UpdatedAt = DateTime.UtcNow;
                }

                var version = BuildVersion(tenant.UpdatedAt);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantCaseDefaultsUpdated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        PreviousWorkflowKey = previousWorkflowKey,
                        PreviousTemplateKey = previousTemplateKey,
                        CurrentWorkflowKey = tenant.DefaultWorkflowKey,
                        CurrentTemplateKey = tenant.DefaultTemplateKey,
                        DefaultsChanged = defaultsChanged,
                        Version = version,
                        ChangedAt = DateTime.UtcNow
                    },
                    token);

                response = MapCaseDefaults(tenant);
            },
            cancellationToken);

        return response!;
    }

    private async Task<Organization> LoadAuthorizedTenantAsync(
        Guid tenantId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var tenant = await _organizationRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant is null)
        {
            throw new TenantAccessDeniedException();
        }

        var hasTenantAdminRole = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!hasTenantAdminRole)
        {
            throw new TenantAccessDeniedException();
        }

        return tenant;
    }

    private static void EnsureCaseDefaultsMutableState(Organization tenant)
    {
        if (tenant.Status != TenantStatus.Terminated)
        {
            return;
        }

        throw new TenantOnboardingStateException(
            "Case defaults cannot be modified for terminated tenants.");
    }

    private static TenantCaseDefaultsResponse MapCaseDefaults(Organization tenant)
    {
        return new TenantCaseDefaultsResponse
        {
            TenantId = tenant.Id,
            DefaultWorkflowKey = tenant.DefaultWorkflowKey,
            DefaultTemplateKey = tenant.DefaultTemplateKey,
            Version = BuildVersion(tenant.UpdatedAt),
            UpdatedAt = tenant.UpdatedAt
        };
    }

    private static long BuildVersion(DateTime updatedAt)
    {
        return updatedAt.ToUniversalTime().Ticks;
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
            Metadata = JsonSerializer.Serialize(metadata)
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}
