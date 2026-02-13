using System.Text.Json;
using System.Security.Cryptography;
using FluentValidation;
using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class CaseService : ICaseService
{
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly ICaseParticipantRepository _caseParticipantRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCaseRequest> _createCaseValidator;
    private readonly IValidator<SubmitCaseIntakeRequest> _submitCaseIntakeValidator;
    private readonly IValidator<UpdateCaseDetailsRequest> _updateCaseDetailsValidator;
    private readonly IValidator<UpdateCaseLifecycleRequest> _updateCaseLifecycleValidator;
    private readonly IValidator<InviteCaseParticipantRequest> _inviteCaseParticipantValidator;
    private readonly IValidator<AcceptCaseParticipantInvitationRequest> _acceptCaseParticipantInvitationValidator;
    private readonly IValidator<UpdateCaseParticipantRoleRequest> _updateCaseParticipantRoleValidator;

    public CaseService(
        IOrganizationRepository organizationRepository,
        IUserRoleRepository userRoleRepository,
        IUserRepository userRepository,
        ICaseRepository caseRepository,
        ICaseParticipantRepository caseParticipantRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCaseRequest> createCaseValidator,
        IValidator<SubmitCaseIntakeRequest> submitCaseIntakeValidator,
        IValidator<UpdateCaseDetailsRequest> updateCaseDetailsValidator,
        IValidator<UpdateCaseLifecycleRequest> updateCaseLifecycleValidator,
        IValidator<InviteCaseParticipantRequest> inviteCaseParticipantValidator,
        IValidator<AcceptCaseParticipantInvitationRequest> acceptCaseParticipantInvitationValidator,
        IValidator<UpdateCaseParticipantRoleRequest> updateCaseParticipantRoleValidator)
    {
        _organizationRepository = organizationRepository;
        _userRoleRepository = userRoleRepository;
        _userRepository = userRepository;
        _caseRepository = caseRepository;
        _caseParticipantRepository = caseParticipantRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _createCaseValidator = createCaseValidator;
        _submitCaseIntakeValidator = submitCaseIntakeValidator;
        _updateCaseDetailsValidator = updateCaseDetailsValidator;
        _updateCaseLifecycleValidator = updateCaseLifecycleValidator;
        _inviteCaseParticipantValidator = inviteCaseParticipantValidator;
        _acceptCaseParticipantInvitationValidator = acceptCaseParticipantInvitationValidator;
        _updateCaseParticipantRoleValidator = updateCaseParticipantRoleValidator;
    }

    public async Task<CreateCaseResponse> CreateCaseAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateCaseRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _createCaseValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        CreateCaseResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var tenant = await LoadAuthorizedTenantAsync(tenantId, actorUserId, token);

                EnsureCaseCreationState(tenant);

                var nextSequence = await _caseRepository.GetNextCaseSequenceAsync(tenantId, token);
                var nowUtc = DateTime.UtcNow;
                var caseEntity = new Case
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseNumber = $"CASE-{nextSequence:D5}",
                    DeceasedFullName = request.DeceasedFullName.Trim(),
                    DateOfDeath = request.DateOfDeath.Date,
                    CaseType = NormalizeCaseType(request.CaseType),
                    Urgency = NormalizeUrgency(request.Urgency),
                    Status = CaseStatus.Draft,
                    Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    ManagerUserId = actorUserId,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                await _caseRepository.CreateAsync(caseEntity, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseCreated",
                    new
                    {
                        caseEntity.Id,
                        caseEntity.TenantId,
                        caseEntity.CaseNumber,
                        caseEntity.DeceasedFullName,
                        caseEntity.DateOfDeath,
                        caseEntity.CaseType,
                        caseEntity.Urgency,
                        caseEntity.Status,
                        caseEntity.ManagerUserId,
                        caseEntity.CreatedAt
                    },
                    token,
                    caseEntity.Id);

                response = MapCreateCase(caseEntity);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDetailsResponse> GetCaseDetailsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseEntity, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Reader, actorUserId, caseId, "GetCaseDetails", cancellationToken);

        return MapCaseDetails(caseEntity);
    }

    public async Task<CaseDetailsResponse> SubmitCaseIntakeAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        SubmitCaseIntakeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _submitCaseIntakeValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Editor, actorUserId, caseId, "SubmitCaseIntake", cancellationToken);

        CaseDetailsResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseEligibleForIntake(caseEntity);

                var nowUtc = DateTime.UtcNow;
                var previousStatus = caseEntity.Status;
                var normalizedNotes = string.IsNullOrWhiteSpace(request.Notes)
                    ? null
                    : request.Notes.Trim();
                var normalizedPrimaryContactName = request.PrimaryContactName.Trim();
                var normalizedPrimaryContactPhone = request.PrimaryContactPhone.Trim();
                var normalizedRelationship = request.RelationshipToDeceased.Trim();

                caseEntity.IntakeData = JsonSerializer.Serialize(
                    new
                    {
                        PrimaryContactName = normalizedPrimaryContactName,
                        PrimaryContactPhone = normalizedPrimaryContactPhone,
                        RelationshipToDeceased = normalizedRelationship,
                        request.HasWill,
                        request.RequiresLegalSupport,
                        request.RequiresFinancialSupport,
                        Notes = normalizedNotes
                    });
                caseEntity.IntakeCompletedAt = nowUtc;
                caseEntity.IntakeCompletedByUserId = actorUserId;
                caseEntity.UpdatedAt = nowUtc;

                if (caseEntity.Status == CaseStatus.Draft)
                {
                    caseEntity.Status = CaseStatus.Intake;
                    await WriteAuditEventAsync(
                        actorUserId,
                        "CaseStatusChanged",
                        new
                        {
                            CaseId = caseEntity.Id,
                            PreviousStatus = previousStatus.ToString(),
                            NewStatus = caseEntity.Status.ToString(),
                            Reason = "Structured intake submitted",
                            ChangedAt = nowUtc
                        },
                        token,
                        caseEntity.Id);
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseIntakeSubmitted",
                    new
                    {
                        CaseId = caseEntity.Id,
                        SubmittedAt = nowUtc,
                        SubmittedByUserId = actorUserId,
                        PrimaryContactName = normalizedPrimaryContactName,
                        PrimaryContactPhone = normalizedPrimaryContactPhone,
                        RelationshipToDeceased = normalizedRelationship,
                        request.HasWill,
                        request.RequiresLegalSupport,
                        request.RequiresFinancialSupport,
                        Notes = normalizedNotes
                    },
                    token,
                    caseEntity.Id);

                response = MapCaseDetails(caseEntity);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDetailsResponse> UpdateCaseDetailsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UpdateCaseDetailsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateCaseDetailsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Editor, actorUserId, caseId, "UpdateCaseDetails", cancellationToken);

        CaseDetailsResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);

                EnsureCaseDetailsMutable(caseEntity);

                var nowUtc = DateTime.UtcNow;
                var previousState = new
                {
                    caseEntity.DeceasedFullName,
                    caseEntity.DateOfDeath,
                    caseEntity.CaseType,
                    caseEntity.Urgency,
                    caseEntity.Notes
                };

                if (request.DeceasedFullName is not null)
                {
                    caseEntity.DeceasedFullName = request.DeceasedFullName.Trim();
                }

                if (request.DateOfDeath.HasValue)
                {
                    caseEntity.DateOfDeath = request.DateOfDeath.Value.Date;
                }

                if (request.CaseType is not null)
                {
                    caseEntity.CaseType = NormalizeCaseType(request.CaseType);
                }

                if (request.Urgency is not null)
                {
                    caseEntity.Urgency = NormalizeUrgency(request.Urgency);
                }

                if (request.Notes is not null)
                {
                    caseEntity.Notes = string.IsNullOrWhiteSpace(request.Notes)
                        ? null
                        : request.Notes.Trim();
                }

                caseEntity.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDetailsUpdated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        Previous = previousState,
                        Current = new
                        {
                            caseEntity.DeceasedFullName,
                            caseEntity.DateOfDeath,
                            caseEntity.CaseType,
                            caseEntity.Urgency,
                            caseEntity.Notes
                        },
                        UpdatedAt = nowUtc
                    },
                    token,
                    caseEntity.Id);

                response = MapCaseDetails(caseEntity);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDetailsResponse> UpdateCaseLifecycleAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UpdateCaseLifecycleRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateCaseLifecycleValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "UpdateCaseLifecycle", cancellationToken);

        CaseDetailsResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);

                var targetStatus = ParseCaseStatus(request.TargetStatus);
                EnsureLifecycleTransitionAllowed(caseEntity.Status, targetStatus);

                var nowUtc = DateTime.UtcNow;
                var previousStatus = caseEntity.Status;

                caseEntity.Status = targetStatus;
                caseEntity.UpdatedAt = nowUtc;
                if (targetStatus == CaseStatus.Closed)
                {
                    caseEntity.ClosedAt = nowUtc;
                }

                if (targetStatus == CaseStatus.Archived)
                {
                    caseEntity.ArchivedAt = nowUtc;
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseStatusChanged",
                    new
                    {
                        CaseId = caseEntity.Id,
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = targetStatus.ToString(),
                        Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim(),
                        ChangedAt = nowUtc
                    },
                    token,
                    caseEntity.Id);

                response = MapCaseDetails(caseEntity);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseMilestonesResponse> GetCaseMilestonesAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseEntity, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Reader, actorUserId, caseId, "GetCaseMilestones", cancellationToken);
        var auditEvents = await _auditRepository.GetByCaseIdAsync(caseId, cancellationToken);

        var milestones = auditEvents
            .Where(x => x.EventType is "CaseCreated" or "CaseStatusChanged")
            .Select(
                auditEvent =>
                {
                    var status = ExtractStatusFromAudit(auditEvent);
                    var description = BuildMilestoneDescription(auditEvent);

                    return new CaseMilestoneResponse
                    {
                        EventType = auditEvent.EventType,
                        Status = status,
                        Description = description,
                        ActorUserId = auditEvent.ActorUserId,
                        OccurredAt = auditEvent.CreatedAt
                    };
                })
            .OrderBy(x => x.OccurredAt)
            .ToList();

        return new CaseMilestonesResponse
        {
            CaseId = caseEntity.Id,
            CaseNumber = caseEntity.CaseNumber,
            Milestones = milestones
        };
    }

    public async Task<InviteCaseParticipantResponse> InviteCaseParticipantAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        InviteCaseParticipantRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _inviteCaseParticipantValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "InviteCaseParticipant", cancellationToken);

        InviteCaseParticipantResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseActiveForCollaboration(caseEntity);

                var nowUtc = DateTime.UtcNow;
                var normalizedEmail = request.Email.Trim().ToLowerInvariant();
                var role = ParseCaseRole(request.Role);

                await _caseParticipantRepository.ExpirePendingInvitesAsync(
                    caseId,
                    normalizedEmail,
                    nowUtc,
                    token);

                if (await _caseParticipantRepository.HasActivePendingInviteAsync(caseId, normalizedEmail, nowUtc, token))
                {
                    throw new CaseStateException(
                        "An active participant invitation already exists for this email.");
                }

                var invitationToken = Guid.NewGuid().ToString("N");
                var participant = new CaseParticipant
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseId = caseId,
                    Email = normalizedEmail,
                    Role = role,
                    Status = CaseParticipantStatus.Pending,
                    TokenHash = HashToken(invitationToken),
                    InvitedByUserId = actorUserId,
                    ExpiresAt = nowUtc.AddDays(request.ExpirationDays),
                    CreatedAt = nowUtc
                };

                await _caseParticipantRepository.CreateAsync(participant, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseParticipantInvited",
                    new
                    {
                        CaseId = caseEntity.Id,
                        ParticipantId = participant.Id,
                        participant.Email,
                        Role = participant.Role.ToString(),
                        Status = participant.Status.ToString(),
                        participant.ExpiresAt
                    },
                    token,
                    caseEntity.Id);

                response = new InviteCaseParticipantResponse
                {
                    Participant = MapParticipant(participant),
                    InvitationToken = invitationToken
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseParticipantResponse> AcceptCaseParticipantInvitationAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid participantId,
        AcceptCaseParticipantInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _acceptCaseParticipantInvitationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        CaseParticipantResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var actorUser = await _userRepository.GetByIdAsync(actorUserId, token);
                if (actorUser is null || actorUser.OrgId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseActiveForCollaboration(caseEntity);

                var participant = await _caseParticipantRepository.GetByIdAsync(participantId, token);
                if (participant is null || participant.CaseId != caseId || participant.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                if (participant.Status != CaseParticipantStatus.Pending)
                {
                    throw new CaseStateException("The invitation is no longer pending.");
                }

                var nowUtc = DateTime.UtcNow;
                if (participant.ExpiresAt <= nowUtc)
                {
                    throw new CaseStateException("The invitation has expired.");
                }

                if (!string.Equals(
                        participant.Email,
                        actorUser.Email,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new TenantAccessDeniedException();
                }

                if (!VerifyToken(request.InvitationToken, participant.TokenHash))
                {
                    throw new CaseStateException("The invitation token is invalid.");
                }

                participant.Status = CaseParticipantStatus.Accepted;
                participant.ParticipantUserId = actorUserId;
                participant.AcceptedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseParticipantAccepted",
                    new
                    {
                        CaseId = caseEntity.Id,
                        ParticipantId = participant.Id,
                        participant.Email,
                        Role = participant.Role.ToString(),
                        participant.Status,
                        participant.AcceptedAt
                    },
                    token,
                    caseEntity.Id);

                response = MapParticipant(participant);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseParticipantResponse> UpdateCaseParticipantRoleAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid participantId,
        UpdateCaseParticipantRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateCaseParticipantRoleValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "UpdateCaseParticipantRole", cancellationToken);

        CaseParticipantResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseActiveForCollaboration(caseEntity);

                var participant = await _caseParticipantRepository.GetByIdAsync(participantId, token);
                if (participant is null || participant.CaseId != caseId || participant.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                if (participant.Status is CaseParticipantStatus.Expired or CaseParticipantStatus.Revoked)
                {
                    throw new CaseStateException("Role cannot be updated for inactive participant invitations.");
                }

                var targetRole = ParseCaseRole(request.Role);
                var previousRole = participant.Role;
                participant.Role = targetRole;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseParticipantRoleUpdated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        ParticipantId = participant.Id,
                        participant.Email,
                        PreviousRole = previousRole.ToString(),
                        NewRole = participant.Role.ToString(),
                        participant.Status
                    },
                    token,
                    caseEntity.Id);

                response = MapParticipant(participant);
            },
            cancellationToken);

        return response!;
    }

    private async Task<CaseRole?> ResolveEffectiveCaseRoleAsync(
        Guid tenantId,
        Guid actorUserId,
        Case caseEntity,
        CancellationToken cancellationToken)
    {
        // Case manager gets Manager role
        if (caseEntity.ManagerUserId == actorUserId)
        {
            return CaseRole.Manager;
        }

        // Tenant admin gets Manager-equivalent access
        var isTenantAdmin = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (isTenantAdmin)
        {
            return CaseRole.Manager;
        }

        // Check accepted case participant role
        var participant = await _caseParticipantRepository.GetAcceptedParticipantAsync(
            caseEntity.Id,
            actorUserId,
            cancellationToken);

        return participant?.Role;
    }

    private async Task EnsureCaseRoleAsync(
        CaseRole? effectiveRole,
        CaseRole minimumRole,
        Guid actorUserId,
        Guid caseId,
        string attemptedAction,
        CancellationToken cancellationToken)
    {
        if (effectiveRole.HasValue && effectiveRole.Value <= minimumRole)
        {
            return;
        }

        var reasonCode = effectiveRole.HasValue ? "ROLE_INSUFFICIENT" : "NO_CASE_ACCESS";
        await WriteAccessDeniedAuditAsync(
            actorUserId,
            caseId,
            attemptedAction,
            minimumRole.ToString(),
            effectiveRole?.ToString(),
            reasonCode,
            cancellationToken);

        throw new CaseAccessDeniedException(
            actorUserId,
            caseId,
            attemptedAction,
            minimumRole.ToString(),
            effectiveRole?.ToString(),
            reasonCode);
    }

    private Task WriteAccessDeniedAuditAsync(
        Guid actorUserId,
        Guid caseId,
        string attemptedAction,
        string requiredRole,
        string? actualRole,
        string reasonCode,
        CancellationToken cancellationToken)
    {
        return _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseAccessDenied",
                    new
                    {
                        ActorUserId = actorUserId,
                        CaseId = caseId,
                        AttemptedAction = attemptedAction,
                        RequiredRole = requiredRole,
                        ActualRole = actualRole,
                        ReasonCode = reasonCode,
                        DeniedAt = DateTime.UtcNow
                    },
                    token,
                    caseId);
            },
            cancellationToken);
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

        var isTenantAdmin = await _userRoleRepository.HasRoleAsync(
            actorUserId,
            tenantId,
            PlatformRole.AgencyAdmin,
            cancellationToken);

        if (!isTenantAdmin)
        {
            throw new TenantAccessDeniedException();
        }

        return tenant;
    }

    private async Task<Case> LoadCaseForTenantAsync(
        Guid tenantId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseEntity = await _caseRepository.GetByIdAsync(caseId, cancellationToken);
        if (caseEntity is null || caseEntity.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        return caseEntity;
    }

    private static void EnsureCaseCreationState(Organization tenant)
    {
        if (tenant.Status != TenantStatus.Active)
        {
            throw new CaseStateException(
                "Cases can only be created for tenants with an active subscription.");
        }
    }

    private static void EnsureCaseEligibleForIntake(Case caseEntity)
    {
        if (caseEntity.Status is not (CaseStatus.Draft or CaseStatus.Intake))
        {
            throw new CaseStateException(
                "Structured intake can only be submitted while a case is in Draft or Intake state.");
        }
    }

    private static void EnsureCaseDetailsMutable(Case caseEntity)
    {
        if (caseEntity.Status is CaseStatus.Archived or CaseStatus.Cancelled)
        {
            throw new CaseStateException(
                "Case details cannot be updated once a case is archived or cancelled.");
        }
    }

    private static void EnsureCaseActiveForCollaboration(Case caseEntity)
    {
        if (caseEntity.Status != CaseStatus.Active)
        {
            throw new CaseStateException(
                "Participant invitations and role assignments are only allowed for active cases.");
        }
    }

    private static CaseStatus ParseCaseStatus(string value)
    {
        if (Enum.TryParse<CaseStatus>(value.Trim(), ignoreCase: true, out var status))
        {
            return status;
        }

        throw new CaseStateException("The requested case status is not recognized.");
    }

    private static CaseRole ParseCaseRole(string value)
    {
        if (Enum.TryParse<CaseRole>(value.Trim(), ignoreCase: true, out var role))
        {
            return role;
        }

        throw new CaseStateException("The requested case role is not recognized.");
    }

    private static void EnsureLifecycleTransitionAllowed(CaseStatus currentStatus, CaseStatus targetStatus)
    {
        var isAllowed = currentStatus switch
        {
            CaseStatus.Draft => targetStatus is CaseStatus.Intake or CaseStatus.Active,
            CaseStatus.Intake => targetStatus == CaseStatus.Active,
            CaseStatus.Active => targetStatus == CaseStatus.Review,
            CaseStatus.Review => targetStatus == CaseStatus.Closed,
            CaseStatus.Closed => targetStatus == CaseStatus.Archived,
            _ => false
        };

        if (!isAllowed)
        {
            throw new CaseStateException(
                $"Invalid lifecycle transition from {currentStatus} to {targetStatus}.");
        }
    }

    private static string NormalizeCaseType(string? caseType)
    {
        return string.IsNullOrWhiteSpace(caseType)
            ? "GENERAL"
            : caseType.Trim().ToUpperInvariant();
    }

    private static string NormalizeUrgency(string? urgency)
    {
        return string.IsNullOrWhiteSpace(urgency)
            ? "NORMAL"
            : urgency.Trim().ToUpperInvariant();
    }

    private static CreateCaseResponse MapCreateCase(Case caseEntity)
    {
        return new CreateCaseResponse
        {
            CaseId = caseEntity.Id,
            TenantId = caseEntity.TenantId,
            CaseNumber = caseEntity.CaseNumber,
            DeceasedFullName = caseEntity.DeceasedFullName,
            DateOfDeath = caseEntity.DateOfDeath,
            CaseType = caseEntity.CaseType,
            Urgency = caseEntity.Urgency,
            Status = caseEntity.Status,
            ManagerUserId = caseEntity.ManagerUserId,
            CreatedAt = caseEntity.CreatedAt
        };
    }

    private static CaseDetailsResponse MapCaseDetails(Case caseEntity)
    {
        return new CaseDetailsResponse
        {
            CaseId = caseEntity.Id,
            TenantId = caseEntity.TenantId,
            CaseNumber = caseEntity.CaseNumber,
            DeceasedFullName = caseEntity.DeceasedFullName,
            DateOfDeath = caseEntity.DateOfDeath,
            CaseType = caseEntity.CaseType,
            Urgency = caseEntity.Urgency,
            Notes = caseEntity.Notes,
            Status = caseEntity.Status,
            ManagerUserId = caseEntity.ManagerUserId,
            CreatedAt = caseEntity.CreatedAt,
            UpdatedAt = caseEntity.UpdatedAt,
            ClosedAt = caseEntity.ClosedAt,
            ArchivedAt = caseEntity.ArchivedAt
        };
    }

    private static CaseParticipantResponse MapParticipant(CaseParticipant participant)
    {
        return new CaseParticipantResponse
        {
            ParticipantId = participant.Id,
            TenantId = participant.TenantId,
            CaseId = participant.CaseId,
            Email = participant.Email,
            Role = participant.Role,
            Status = participant.Status,
            InvitedByUserId = participant.InvitedByUserId,
            ParticipantUserId = participant.ParticipantUserId,
            ExpiresAt = participant.ExpiresAt,
            AcceptedAt = participant.AcceptedAt
        };
    }

    private static CaseStatus? ExtractStatusFromAudit(AuditEvent auditEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(auditEvent.Metadata);
            var root = document.RootElement;
            var statusField = auditEvent.EventType == "CaseStatusChanged"
                ? "NewStatus"
                : "Status";

            if (!root.TryGetProperty(statusField, out var statusValue))
            {
                return null;
            }

            if (statusValue.ValueKind == JsonValueKind.Number && statusValue.TryGetInt32(out var statusNumber))
            {
                return Enum.IsDefined(typeof(CaseStatus), statusNumber)
                    ? (CaseStatus)statusNumber
                    : null;
            }

            if (statusValue.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var statusString = statusValue.GetString();
            if (string.IsNullOrWhiteSpace(statusString))
            {
                return null;
            }

            return Enum.TryParse<CaseStatus>(statusString, ignoreCase: true, out var parsedStatus)
                ? parsedStatus
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string BuildMilestoneDescription(AuditEvent auditEvent)
    {
        if (auditEvent.EventType == "CaseCreated")
        {
            return "Case created";
        }

        if (auditEvent.EventType != "CaseStatusChanged")
        {
            return auditEvent.EventType;
        }

        try
        {
            using var document = JsonDocument.Parse(auditEvent.Metadata);
            var root = document.RootElement;
            var previousStatus = root.TryGetProperty("PreviousStatus", out var prev)
                ? prev.GetString()
                : null;
            var newStatus = root.TryGetProperty("NewStatus", out var next)
                ? next.GetString()
                : null;

            if (!string.IsNullOrWhiteSpace(previousStatus) && !string.IsNullOrWhiteSpace(newStatus))
            {
                return $"Case status changed from {previousStatus} to {newStatus}";
            }
        }
        catch (JsonException)
        {
        }

        return "Case lifecycle updated";
    }

    private static string HashToken(string token)
    {
        var hashBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }

    private static bool VerifyToken(string providedToken, string storedHash)
    {
        var providedHash = HashToken(providedToken);
        var providedBytes = Convert.FromHexString(providedHash);
        var storedBytes = Convert.FromHexString(storedHash);

        return CryptographicOperations.FixedTimeEquals(providedBytes, storedBytes);
    }

    private Task WriteAuditEventAsync(
        Guid actorUserId,
        string eventType,
        object metadata,
        CancellationToken cancellationToken,
        Guid? caseId = null)
    {
        var auditEvent = new AuditEvent
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            ActorUserId = actorUserId,
            EventType = eventType,
            Metadata = JsonSerializer.Serialize(metadata)
        };

        return _auditRepository.CreateAsync(auditEvent, cancellationToken);
    }
}
