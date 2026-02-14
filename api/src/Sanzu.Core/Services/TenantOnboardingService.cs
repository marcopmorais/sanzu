using System.Text.Json;
using System.Security.Cryptography;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Notifications;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class TenantOnboardingService : ITenantOnboardingService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ITenantInvitationRepository _tenantInvitationRepository;
    private readonly ITenantInvitationNotificationSender _tenantInvitationNotificationSender;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateAgencyAccountRequest> _createAgencyAccountValidator;
    private readonly IValidator<UpdateTenantOnboardingProfileRequest> _updateProfileValidator;
    private readonly IValidator<UpdateTenantOnboardingDefaultsRequest> _updateDefaultsValidator;
    private readonly IValidator<UpdateTenantCaseDefaultsRequest> _updateCaseDefaultsValidator;
    private readonly IValidator<CreateTenantInvitationRequest> _createInvitationValidator;
    private readonly IValidator<CompleteTenantOnboardingRequest> _completeOnboardingValidator;
    private readonly IValidator<ActivateTenantBillingRequest> _activateBillingValidator;

    public TenantOnboardingService(
        IOrganizationRepository organizationRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IAuditRepository auditRepository,
        ITenantInvitationRepository tenantInvitationRepository,
        ITenantInvitationNotificationSender tenantInvitationNotificationSender,
        IUnitOfWork unitOfWork,
        IValidator<CreateAgencyAccountRequest> createAgencyAccountValidator,
        IValidator<UpdateTenantOnboardingProfileRequest> updateProfileValidator,
        IValidator<UpdateTenantOnboardingDefaultsRequest> updateDefaultsValidator,
        IValidator<UpdateTenantCaseDefaultsRequest> updateCaseDefaultsValidator,
        IValidator<CreateTenantInvitationRequest> createInvitationValidator,
        IValidator<CompleteTenantOnboardingRequest> completeOnboardingValidator,
        IValidator<ActivateTenantBillingRequest> activateBillingValidator)
    {
        _organizationRepository = organizationRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _auditRepository = auditRepository;
        _tenantInvitationRepository = tenantInvitationRepository;
        _tenantInvitationNotificationSender = tenantInvitationNotificationSender;
        _unitOfWork = unitOfWork;
        _createAgencyAccountValidator = createAgencyAccountValidator;
        _updateProfileValidator = updateProfileValidator;
        _updateDefaultsValidator = updateDefaultsValidator;
        _updateCaseDefaultsValidator = updateCaseDefaultsValidator;
        _createInvitationValidator = createInvitationValidator;
        _completeOnboardingValidator = completeOnboardingValidator;
        _activateBillingValidator = activateBillingValidator;
    }

    public async Task<CreateAgencyAccountResponse> CreateAgencyAccountAsync(
        CreateAgencyAccountRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createAgencyAccountValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            throw new DuplicateEmailException();
        }

        Organization? organization = null;
        User? user = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                organization = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = request.AgencyName.Trim(),
                    Location = request.Location.Trim(),
                    Status = TenantStatus.Pending
                };
                await _organizationRepository.CreateAsync(organization, token);

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail,
                    FullName = request.FullName.Trim(),
                    OrgId = organization.Id
                };
                await _userRepository.CreateAsync(user, token);

                var role = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    RoleType = PlatformRole.AgencyAdmin,
                    TenantId = organization.Id,
                    GrantedBy = user.Id
                };
                await _userRoleRepository.CreateAsync(role, token);

                var auditEvent = new AuditEvent
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = user.Id,
                    EventType = "TenantCreated",
                    Metadata = JsonSerializer.Serialize(
                        new
                        {
                            OrganizationId = organization.Id,
                            UserId = user.Id,
                            InitialStatus = TenantStatus.Pending.ToString()
                        })
                };
                await _auditRepository.CreateAsync(auditEvent, token);
            },
            cancellationToken);

        return new CreateAgencyAccountResponse
        {
            OrganizationId = organization!.Id,
            UserId = user!.Id,
            TenantStatus = organization.Status
        };
    }

    public async Task<TenantOnboardingProfileResponse> UpdateOnboardingProfileAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantOnboardingProfileRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantOnboardingProfileResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureOnboardingMutableState(tenant);

                tenant.Name = request.AgencyName.Trim();
                tenant.Location = request.Location.Trim();
                tenant.UpdatedAt = DateTime.UtcNow;
                MoveToOnboardingIfPending(tenant);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantOnboardingProfileUpdated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        tenant.Name,
                        tenant.Location,
                        tenant.Status
                    },
                    token);

                response = new TenantOnboardingProfileResponse
                {
                    TenantId = tenant.Id,
                    AgencyName = tenant.Name,
                    Location = tenant.Location,
                    TenantStatus = tenant.Status
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<TenantOnboardingDefaultsResponse> UpdateOnboardingDefaultsAsync(
        Guid tenantId,
        Guid actorUserId,
        UpdateTenantOnboardingDefaultsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateDefaultsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantOnboardingDefaultsResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureOnboardingMutableState(tenant);

                var previousWorkflowKey = tenant.DefaultWorkflowKey;
                var previousTemplateKey = tenant.DefaultTemplateKey;
                tenant.DefaultLocale = request.DefaultLocale.Trim();
                tenant.DefaultTimeZone = request.DefaultTimeZone.Trim();
                tenant.DefaultCurrency = request.DefaultCurrency.Trim().ToUpperInvariant();
                tenant.DefaultWorkflowKey = request.DefaultWorkflowKey?.Trim();
                tenant.DefaultTemplateKey = request.DefaultTemplateKey?.Trim();
                if (!string.Equals(previousWorkflowKey, tenant.DefaultWorkflowKey, StringComparison.Ordinal)
                    || !string.Equals(previousTemplateKey, tenant.DefaultTemplateKey, StringComparison.Ordinal))
                {
                    tenant.CaseDefaultsVersion += 1;
                }
                tenant.UpdatedAt = DateTime.UtcNow;
                MoveToOnboardingIfPending(tenant);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantOnboardingDefaultsUpdated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        tenant.DefaultLocale,
                        tenant.DefaultTimeZone,
                        tenant.DefaultCurrency,
                        tenant.DefaultWorkflowKey,
                        tenant.DefaultTemplateKey,
                        tenant.CaseDefaultsVersion
                    },
                    token);

                response = new TenantOnboardingDefaultsResponse
                {
                    TenantId = tenant.Id,
                    DefaultLocale = tenant.DefaultLocale!,
                    DefaultTimeZone = tenant.DefaultTimeZone!,
                    DefaultCurrency = tenant.DefaultCurrency!,
                    DefaultWorkflowKey = tenant.DefaultWorkflowKey,
                    DefaultTemplateKey = tenant.DefaultTemplateKey
                };
            },
            cancellationToken);

        return response!;
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

                var nowUtc = DateTime.UtcNow;
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
                    tenant.CaseDefaultsVersion += 1;
                }

                tenant.UpdatedAt = nowUtc;

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
                        tenant.CaseDefaultsVersion,
                        DefaultsChanged = defaultsChanged,
                        ChangedAt = nowUtc
                    },
                    token);

                response = MapCaseDefaults(tenant);
            },
            cancellationToken);

        return response!;
    }

    public async Task<TenantInvitationResponse> CreateInvitationAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateTenantInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createInvitationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantInvitationResponse? response = null;
        TenantInvitationNotification? notification = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureOnboardingMutableState(tenant);
                MoveToOnboardingIfPending(tenant);

                var nowUtc = DateTime.UtcNow;
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                tenant.UpdatedAt = nowUtc;

                await _tenantInvitationRepository.ExpirePendingInvitesAsync(
                    tenantId,
                    normalizedEmail,
                    nowUtc,
                    token);

                if (await _userRepository.ExistsByEmailInOrganizationAsync(tenantId, normalizedEmail, token))
                {
                    throw new TenantOnboardingConflictException(
                        "The invitation could not be created with the provided information.");
                }

                if (await _tenantInvitationRepository.HasActivePendingInviteAsync(tenantId, normalizedEmail, nowUtc, token))
                {
                    throw new TenantOnboardingConflictException(
                        "The invitation could not be created with the provided information.");
                }

                var invitationToken = Guid.NewGuid().ToString("N");
                var invitation = new TenantInvitation
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    Email = normalizedEmail,
                    RoleType = request.RoleType,
                    TokenHash = HashToken(invitationToken),
                    ExpiresAt = nowUtc.AddDays(request.ExpirationDays),
                    Status = TenantInvitationStatus.Pending,
                    InvitedBy = actorUserId
                };

                await _tenantInvitationRepository.CreateAsync(invitation, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantInvitationCreated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        InvitationId = invitation.Id,
                        invitation.Email,
                        invitation.RoleType,
                        invitation.Status,
                        invitation.ExpiresAt
                    },
                    token);

                response = new TenantInvitationResponse
                {
                    InvitationId = invitation.Id,
                    TenantId = invitation.TenantId,
                    Email = invitation.Email,
                    RoleType = invitation.RoleType,
                    ExpiresAt = invitation.ExpiresAt,
                    Status = invitation.Status
                };

                notification = new TenantInvitationNotification(
                    invitation.Id,
                    invitation.TenantId,
                    invitation.Email,
                    invitation.RoleType,
                    invitation.ExpiresAt,
                    invitationToken);
            },
            cancellationToken);

        if (notification is not null)
        {
            await _tenantInvitationNotificationSender.SendTenantInviteAsync(notification, cancellationToken);
        }

        return response!;
    }

    public async Task<TenantOnboardingCompletionResponse> CompleteOnboardingAsync(
        Guid tenantId,
        Guid actorUserId,
        CompleteTenantOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _completeOnboardingValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantOnboardingCompletionResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureOnboardingMutableState(tenant);
                EnsureCompletionRequirements(tenant);

                MoveToOnboardingIfPending(tenant);
                tenant.OnboardingCompletedAt ??= DateTime.UtcNow;
                tenant.UpdatedAt = DateTime.UtcNow;

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantOnboardingCompleted",
                    new
                    {
                        OrganizationId = tenant.Id,
                        tenant.OnboardingCompletedAt,
                        tenant.Status
                    },
                    token);

                response = new TenantOnboardingCompletionResponse
                {
                    TenantId = tenant.Id,
                    TenantStatus = tenant.Status,
                    OnboardingCompletedAt = tenant.OnboardingCompletedAt.Value
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<TenantBillingActivationResponse> ActivateBillingAsync(
        Guid tenantId,
        Guid actorUserId,
        ActivateTenantBillingRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _activateBillingValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        TenantBillingActivationResponse? response = null;
        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);
                EnsureBillingActivationRequirements(tenant);

                var nowUtc = DateTime.UtcNow;
                tenant.SubscriptionPlan = request.PlanCode.Trim().ToUpperInvariant();
                tenant.SubscriptionBillingCycle = request.BillingCycle.Trim().ToUpperInvariant();
                tenant.PaymentMethodType = request.PaymentMethodType.Trim().ToUpperInvariant();
                tenant.PaymentMethodReference = request.PaymentMethodReference.Trim();
                tenant.InvoiceProfileLegalName = request.InvoiceProfileLegalName.Trim();
                tenant.InvoiceProfileVatNumber = string.IsNullOrWhiteSpace(request.InvoiceProfileVatNumber)
                    ? null
                    : request.InvoiceProfileVatNumber.Trim().ToUpperInvariant();
                tenant.InvoiceProfileBillingEmail = request.InvoiceProfileBillingEmail.Trim().ToLowerInvariant();
                tenant.InvoiceProfileCountryCode = request.InvoiceProfileCountryCode.Trim().ToUpperInvariant();
                tenant.SubscriptionActivatedAt ??= nowUtc;
                tenant.Status = TenantStatus.Active;
                tenant.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "TenantBillingActivated",
                    new
                    {
                        OrganizationId = tenant.Id,
                        tenant.SubscriptionPlan,
                        tenant.SubscriptionBillingCycle,
                        tenant.PaymentMethodType,
                        tenant.InvoiceProfileBillingEmail,
                        tenant.InvoiceProfileCountryCode,
                        tenant.SubscriptionActivatedAt,
                        tenant.Status
                    },
                    token);

                response = new TenantBillingActivationResponse
                {
                    TenantId = tenant.Id,
                    TenantStatus = tenant.Status,
                    PlanCode = tenant.SubscriptionPlan!,
                    BillingCycle = tenant.SubscriptionBillingCycle!,
                    PaymentMethodType = tenant.PaymentMethodType!,
                    InvoiceProfileBillingEmail = tenant.InvoiceProfileBillingEmail!,
                    SubscriptionActivatedAt = tenant.SubscriptionActivatedAt.Value
                };
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

    private static void EnsureOnboardingMutableState(Organization tenant)
    {
        if (tenant.Status is TenantStatus.Pending or TenantStatus.Onboarding)
        {
            return;
        }

        throw new TenantOnboardingStateException(
            "Onboarding setup can only be modified for tenants in Pending or Onboarding state.");
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

    private static void EnsureCompletionRequirements(Organization tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.DefaultLocale)
            || string.IsNullOrWhiteSpace(tenant.DefaultTimeZone)
            || string.IsNullOrWhiteSpace(tenant.DefaultCurrency))
        {
            throw new TenantOnboardingStateException(
                "Onboarding defaults must be completed before onboarding can be marked complete.");
        }
    }

    private static void EnsureBillingActivationRequirements(Organization tenant)
    {
        if (tenant.Status is not (TenantStatus.Pending or TenantStatus.Onboarding))
        {
            throw new TenantOnboardingStateException(
                "Billing activation can only be completed for tenants in Pending or Onboarding state.");
        }

        if (!tenant.OnboardingCompletedAt.HasValue)
        {
            throw new TenantOnboardingStateException(
                "Onboarding must be completed before billing can be activated.");
        }
    }

    private static void MoveToOnboardingIfPending(Organization tenant)
    {
        if (tenant.Status == TenantStatus.Pending)
        {
            tenant.Status = TenantStatus.Onboarding;
        }
    }

    private static TenantCaseDefaultsResponse MapCaseDefaults(Organization tenant)
    {
        return new TenantCaseDefaultsResponse
        {
            TenantId = tenant.Id,
            DefaultWorkflowKey = tenant.DefaultWorkflowKey,
            DefaultTemplateKey = tenant.DefaultTemplateKey,
            Version = tenant.CaseDefaultsVersion,
            UpdatedAt = tenant.UpdatedAt
        };
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
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
