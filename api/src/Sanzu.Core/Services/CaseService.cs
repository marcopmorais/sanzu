using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
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
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IExtractionCandidateRepository _extractionCandidateRepository;
    private readonly ICaseParticipantRepository _caseParticipantRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCaseRequest> _createCaseValidator;
    private readonly IValidator<SubmitCaseIntakeRequest> _submitCaseIntakeValidator;
    private readonly IValidator<ApplyExtractionDecisionsRequest> _applyExtractionDecisionsValidator;
    private readonly IValidator<GenerateOutboundTemplateRequest> _generateOutboundTemplateValidator;
    private readonly IValidator<UploadCaseDocumentRequest> _uploadCaseDocumentValidator;
    private readonly IValidator<UpdateCaseDocumentClassificationRequest> _updateCaseDocumentClassificationValidator;
    private readonly IValidator<OverrideWorkflowStepReadinessRequest> _overrideWorkflowStepReadinessValidator;
    private readonly IValidator<UpdateWorkflowTaskStatusRequest> _updateWorkflowTaskStatusValidator;
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
        ICaseDocumentRepository caseDocumentRepository,
        IExtractionCandidateRepository extractionCandidateRepository,
        ICaseParticipantRepository caseParticipantRepository,
        IWorkflowStepRepository workflowStepRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCaseRequest> createCaseValidator,
        IValidator<SubmitCaseIntakeRequest> submitCaseIntakeValidator,
        IValidator<ApplyExtractionDecisionsRequest> applyExtractionDecisionsValidator,
        IValidator<GenerateOutboundTemplateRequest> generateOutboundTemplateValidator,
        IValidator<UploadCaseDocumentRequest> uploadCaseDocumentValidator,
        IValidator<UpdateCaseDocumentClassificationRequest> updateCaseDocumentClassificationValidator,
        IValidator<OverrideWorkflowStepReadinessRequest> overrideWorkflowStepReadinessValidator,
        IValidator<UpdateWorkflowTaskStatusRequest> updateWorkflowTaskStatusValidator,
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
        _caseDocumentRepository = caseDocumentRepository;
        _extractionCandidateRepository = extractionCandidateRepository;
        _caseParticipantRepository = caseParticipantRepository;
        _workflowStepRepository = workflowStepRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
        _createCaseValidator = createCaseValidator;
        _submitCaseIntakeValidator = submitCaseIntakeValidator;
        _applyExtractionDecisionsValidator = applyExtractionDecisionsValidator;
        _generateOutboundTemplateValidator = generateOutboundTemplateValidator;
        _uploadCaseDocumentValidator = uploadCaseDocumentValidator;
        _updateCaseDocumentClassificationValidator = updateCaseDocumentClassificationValidator;
        _overrideWorkflowStepReadinessValidator = overrideWorkflowStepReadinessValidator;
        _updateWorkflowTaskStatusValidator = updateWorkflowTaskStatusValidator;
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

    public async Task<GenerateCasePlanResponse> GenerateCasePlanAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "GenerateCasePlan", cancellationToken);
        EnsureCaseHasCompletedIntake(caseForAccess);
        var intakeSnapshot = ParseIntakeSnapshot(caseForAccess.IntakeData!);

        GenerateCasePlanResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseHasCompletedIntake(caseEntity);
                var latestIntakeSnapshot = ParseIntakeSnapshot(caseEntity.IntakeData!);
                var stepBlueprints = BuildPlanBlueprints(latestIntakeSnapshot);
                var nowUtc = DateTime.UtcNow;

                await _workflowStepRepository.DeletePlanByCaseIdAsync(caseEntity.Id, token);

                var stepEntities = stepBlueprints
                    .Select(
                        (blueprint, index) =>
                        {
                            var sequence = index + 1;
                            var dueDate = BuildStepDueDate(nowUtc, sequence);

                            return new WorkflowStepInstance
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CaseId = caseEntity.Id,
                                StepKey = blueprint.StepKey,
                                Title = blueprint.Title,
                                Sequence = sequence,
                                Status = blueprint.DependsOnKeys.Count == 0
                                    ? WorkflowStepStatus.Ready
                                    : WorkflowStepStatus.Blocked,
                                AssignedUserId = caseEntity.ManagerUserId,
                                DueDate = dueDate,
                                DeadlineSource = BuildDeadlineSource(sequence),
                                CreatedAt = nowUtc,
                                UpdatedAt = nowUtc
                            };
                        })
                    .ToList();

                await _workflowStepRepository.CreateStepsAsync(stepEntities, token);

                var stepIdByKey = stepEntities.ToDictionary(x => x.StepKey, x => x.Id, StringComparer.OrdinalIgnoreCase);
                var dependencyEntities = stepBlueprints
                    .SelectMany(
                        blueprint => blueprint.DependsOnKeys.Select(
                            dependsOnKey => new WorkflowStepDependency
                            {
                                Id = Guid.NewGuid(),
                                TenantId = tenantId,
                                CaseId = caseEntity.Id,
                                StepId = stepIdByKey[blueprint.StepKey],
                                DependsOnStepId = stepIdByKey[dependsOnKey],
                                CreatedAt = nowUtc
                            }))
                    .ToList();

                await _workflowStepRepository.CreateDependenciesAsync(dependencyEntities, token);

                if (caseEntity.Status == CaseStatus.Intake)
                {
                    var previousStatus = caseEntity.Status;
                    caseEntity.Status = CaseStatus.Active;
                    caseEntity.UpdatedAt = nowUtc;

                    await WriteAuditEventAsync(
                        actorUserId,
                        "CaseStatusChanged",
                        new
                        {
                            CaseId = caseEntity.Id,
                            PreviousStatus = previousStatus.ToString(),
                            NewStatus = caseEntity.Status.ToString(),
                            Reason = "Case plan generated",
                            ChangedAt = nowUtc
                        },
                        token,
                        caseEntity.Id);
                }
                else
                {
                    caseEntity.UpdatedAt = nowUtc;
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "CasePlanGenerated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        GeneratedAt = nowUtc,
                        StepCount = stepEntities.Count,
                        DependencyCount = dependencyEntities.Count,
                        StepKeys = stepEntities.Select(x => x.StepKey).ToArray(),
                        IntakeFlags = new
                        {
                            intakeSnapshot.HasWill,
                            intakeSnapshot.RequiresLegalSupport,
                            intakeSnapshot.RequiresFinancialSupport
                        }
                    },
                    token,
                    caseEntity.Id);

                await WriteAuditEventAsync(
                    actorUserId,
                    "WorkflowTaskOwnershipInitialized",
                    new
                    {
                        CaseId = caseEntity.Id,
                        InitializedAt = nowUtc,
                        DefaultOwnerUserId = caseEntity.ManagerUserId,
                        Owners = stepEntities.Select(
                            x => new
                            {
                                x.Id,
                                x.StepKey,
                                x.AssignedUserId
                            })
                    },
                    token,
                    caseEntity.Id);

                var dependencyMap = dependencyEntities
                    .GroupBy(x => x.StepId)
                    .ToDictionary(
                        x => x.Key,
                        x => (IReadOnlyList<Guid>)x.Select(d => d.DependsOnStepId).ToList());

                response = new GenerateCasePlanResponse
                {
                    CaseId = caseEntity.Id,
                    GeneratedAt = nowUtc,
                    Steps = stepEntities
                        .OrderBy(x => x.Sequence)
                        .Select(
                            step => new CasePlanStepResponse
                            {
                                StepId = step.Id,
                                StepKey = step.StepKey,
                                Title = step.Title,
                                Sequence = step.Sequence,
                                Status = step.Status,
                                DependsOnStepIds = dependencyMap.TryGetValue(step.Id, out var dependsOn)
                                    ? dependsOn
                                    : Array.Empty<Guid>()
                            })
                        .ToList()
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDocumentUploadResponse> UploadCaseDocumentAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UploadCaseDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _uploadCaseDocumentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Editor, actorUserId, caseId, "UploadCaseDocument", cancellationToken);

        CaseDocumentUploadResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                var nowUtc = DateTime.UtcNow;
                var contentBytes = DecodeDocumentContent(request.ContentBase64);

                var document = new CaseDocument
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseId = caseEntity.Id,
                    FileName = request.FileName.Trim(),
                    ContentType = request.ContentType.Trim(),
                    SizeBytes = contentBytes.LongLength,
                    Content = contentBytes,
                    CurrentVersionNumber = 1,
                    Classification = CaseDocumentClassification.Optional,
                    UploadedByUserId = actorUserId,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc
                };

                var initialVersion = new CaseDocumentVersion
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseId = caseEntity.Id,
                    DocumentId = document.Id,
                    VersionNumber = 1,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    SizeBytes = document.SizeBytes,
                    Content = contentBytes,
                    UploadedByUserId = actorUserId,
                    CreatedAt = nowUtc
                };

                await _caseDocumentRepository.CreateAsync(document, token);
                await _caseDocumentRepository.CreateVersionAsync(initialVersion, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDocumentUploaded",
                    new
                    {
                        CaseId = caseEntity.Id,
                        DocumentId = document.Id,
                        document.FileName,
                        document.ContentType,
                        document.SizeBytes,
                        document.CurrentVersionNumber,
                        Classification = document.Classification.ToString(),
                        document.UploadedByUserId,
                        UploadedAt = nowUtc
                    },
                    token,
                    caseEntity.Id);

                response = MapDocumentUpload(document);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDocumentDownloadResponse> DownloadCaseDocumentAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);

        var document = await _caseDocumentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        var requiredRole = document.Classification == CaseDocumentClassification.Restricted
            ? CaseRole.Manager
            : CaseRole.Reader;
        await EnsureCaseRoleAsync(effectiveRole, requiredRole, actorUserId, caseId, "DownloadCaseDocument", cancellationToken);

        CaseDocumentDownloadResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var downloadedAt = DateTime.UtcNow;
                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDocumentDownloaded",
                    new
                    {
                        CaseId = caseForAccess.Id,
                        DocumentId = document.Id,
                        document.FileName,
                        document.ContentType,
                        document.SizeBytes,
                        document.CurrentVersionNumber,
                        Classification = document.Classification.ToString(),
                        DownloadedByUserId = actorUserId,
                        DownloadedAt = downloadedAt
                    },
                    token,
                    caseForAccess.Id);

                response = MapDocumentDownload(document);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDocumentUploadResponse> UploadCaseDocumentVersionAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        UploadCaseDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _uploadCaseDocumentValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);

        var documentForAccess = await _caseDocumentRepository.GetByIdAsync(documentId, cancellationToken);
        if (documentForAccess is null || documentForAccess.CaseId != caseForAccess.Id || documentForAccess.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        var requiredRole = documentForAccess.Classification == CaseDocumentClassification.Restricted
            ? CaseRole.Manager
            : CaseRole.Editor;
        await EnsureCaseRoleAsync(effectiveRole, requiredRole, actorUserId, caseId, "UploadCaseDocumentVersion", cancellationToken);

        CaseDocumentUploadResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var nowUtc = DateTime.UtcNow;
                var contentBytes = DecodeDocumentContent(request.ContentBase64);

                var document = await _caseDocumentRepository.GetByIdAsync(documentId, token);
                if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                var previousVersionNumber = document.CurrentVersionNumber;
                var nextVersionNumber = previousVersionNumber + 1;

                var version = new CaseDocumentVersion
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseId = caseForAccess.Id,
                    DocumentId = document.Id,
                    VersionNumber = nextVersionNumber,
                    FileName = request.FileName.Trim(),
                    ContentType = request.ContentType.Trim(),
                    SizeBytes = contentBytes.LongLength,
                    Content = contentBytes,
                    UploadedByUserId = actorUserId,
                    CreatedAt = nowUtc
                };

                document.FileName = version.FileName;
                document.ContentType = version.ContentType;
                document.SizeBytes = version.SizeBytes;
                document.Content = version.Content;
                document.CurrentVersionNumber = nextVersionNumber;
                document.UploadedByUserId = actorUserId;
                document.UpdatedAt = nowUtc;

                await _caseDocumentRepository.CreateVersionAsync(version, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDocumentVersionUploaded",
                    new
                    {
                        CaseId = caseForAccess.Id,
                        DocumentId = document.Id,
                        VersionId = version.Id,
                        PreviousVersionNumber = previousVersionNumber,
                        NewVersionNumber = nextVersionNumber,
                        version.FileName,
                        version.ContentType,
                        version.SizeBytes,
                        Classification = document.Classification.ToString(),
                        UploadedByUserId = actorUserId,
                        UploadedAt = nowUtc
                    },
                    token,
                    caseForAccess.Id);

                response = MapDocumentUpload(document);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseDocumentVersionHistoryResponse> GetCaseDocumentVersionsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);

        var document = await _caseDocumentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        var requiredRole = document.Classification == CaseDocumentClassification.Restricted
            ? CaseRole.Manager
            : CaseRole.Reader;
        await EnsureCaseRoleAsync(effectiveRole, requiredRole, actorUserId, caseId, "GetCaseDocumentVersions", cancellationToken);

        var versions = await _caseDocumentRepository.GetVersionsAsync(documentId, cancellationToken);
        return MapDocumentVersionHistory(document, versions);
    }

    public async Task<CaseDocumentClassificationResponse> UpdateCaseDocumentClassificationAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        UpdateCaseDocumentClassificationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateCaseDocumentClassificationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "UpdateCaseDocumentClassification", cancellationToken);

        var targetClassification = ParseCaseDocumentClassification(request.Classification);
        CaseDocumentClassificationResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var document = await _caseDocumentRepository.GetByIdAsync(documentId, token);
                if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                var nowUtc = DateTime.UtcNow;
                var previousClassification = document.Classification;
                document.Classification = targetClassification;
                document.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDocumentClassificationUpdated",
                    new
                    {
                        CaseId = caseForAccess.Id,
                        DocumentId = document.Id,
                        PreviousClassification = previousClassification.ToString(),
                        NewClassification = targetClassification.ToString(),
                        UpdatedByUserId = actorUserId,
                        UpdatedAt = nowUtc
                    },
                    token,
                    caseForAccess.Id);

                response = MapDocumentClassification(document);
            },
            cancellationToken);

        return response!;
    }

    public async Task<GenerateOutboundTemplateResponse> GenerateOutboundTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        GenerateOutboundTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _generateOutboundTemplateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "GenerateOutboundTemplate", cancellationToken);

        GenerateOutboundTemplateResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseHasCompletedIntake(caseEntity);

                var templateKey = ParseOutboundTemplateKey(request.TemplateKey);
                var generatedAt = DateTime.UtcNow;
                var mappedFields = BuildOutboundTemplateFields(templateKey, caseEntity);
                var templateContent = BuildOutboundTemplateContent(templateKey, mappedFields);
                var fileName = $"{templateKey}-{caseEntity.CaseNumber}-{generatedAt:yyyyMMddHHmmss}.txt";
                var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(templateContent));

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseOutboundTemplateGenerated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        TemplateKey = templateKey,
                        FileName = fileName,
                        ContentType = "text/plain",
                        GeneratedByUserId = actorUserId,
                        GeneratedAt = generatedAt,
                        MappedFields = mappedFields
                    },
                    token,
                    caseEntity.Id);

                response = new GenerateOutboundTemplateResponse
                {
                    CaseId = caseEntity.Id,
                    TemplateKey = templateKey,
                    FileName = fileName,
                    ContentType = "text/plain",
                    ContentBase64 = contentBase64,
                    GeneratedAt = generatedAt,
                    MappedFields = mappedFields
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<ExtractDocumentCandidatesResponse> ExtractDocumentCandidatesAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);

        var document = await _caseDocumentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        var requiredRole = document.Classification == CaseDocumentClassification.Restricted
            ? CaseRole.Manager
            : CaseRole.Editor;
        await EnsureCaseRoleAsync(effectiveRole, requiredRole, actorUserId, caseId, "ExtractDocumentCandidates", cancellationToken);

        if (!IsSupportedExtractionContentType(document.ContentType))
        {
            throw new CaseStateException("Document content type is not supported for extraction.");
        }

        var extractedAt = DateTime.UtcNow;
        var contentText = System.Text.Encoding.UTF8.GetString(document.Content);
        var candidates = BuildExtractionCandidates(tenantId, caseForAccess.Id, document, contentText, extractedAt);

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                await _extractionCandidateRepository.CreateRangeAsync(candidates, token);

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseDocumentExtractionCompleted",
                    new
                    {
                        CaseId = caseForAccess.Id,
                        DocumentId = document.Id,
                        document.CurrentVersionNumber,
                        ExtractedByUserId = actorUserId,
                        ExtractedAt = extractedAt,
                        CandidateCount = candidates.Count,
                        Candidates = candidates.Select(
                            candidate => new
                            {
                                candidate.FieldKey,
                                candidate.CandidateValue,
                                candidate.ConfidenceScore,
                                Status = candidate.Status.ToString()
                            })
                    },
                    token,
                    caseForAccess.Id);
            },
            cancellationToken);

        return new ExtractDocumentCandidatesResponse
        {
            CaseId = caseForAccess.Id,
            DocumentId = document.Id,
            SourceVersionNumber = document.CurrentVersionNumber,
            ExtractedAt = extractedAt,
            Candidates = candidates.Select(MapExtractionCandidate).ToList()
        };
    }

    public async Task<ApplyExtractionDecisionsResponse> ApplyExtractionDecisionsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        ApplyExtractionDecisionsRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _applyExtractionDecisionsValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var duplicateDecision = request.Decisions
            .GroupBy(x => x.CandidateId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateDecision is not null)
        {
            throw new ValidationException(
                new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(ApplyExtractionDecisionsRequest.Decisions),
                        $"Candidate {duplicateDecision.Key} appears more than once in the review payload.")
                });
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "ApplyExtractionDecisions", cancellationToken);

        var document = await _caseDocumentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null || document.CaseId != caseForAccess.Id || document.TenantId != tenantId)
        {
            throw new TenantAccessDeniedException();
        }

        var candidates = await _extractionCandidateRepository.GetByDocumentIdAsync(documentId, cancellationToken);
        var pendingById = candidates
            .Where(x => x.Status == ExtractionCandidateStatus.Pending)
            .ToDictionary(x => x.Id, x => x);
        if (pendingById.Count == 0)
        {
            throw new CaseStateException("No pending extraction candidates are available for review.");
        }

        foreach (var decision in request.Decisions)
        {
            if (!pendingById.ContainsKey(decision.CandidateId))
            {
                throw new CaseStateException("One or more extraction candidates are not pending or do not exist.");
            }
        }

        ApplyExtractionDecisionsResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                var reviewCandidates = (await _extractionCandidateRepository.GetByDocumentIdAsync(documentId, token))
                    .Where(x => x.Status == ExtractionCandidateStatus.Pending)
                    .ToDictionary(x => x.Id, x => x);

                var reviewedAt = DateTime.UtcNow;
                var approvedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var decisionAuditItems = new List<object>();
                var rejectedCount = 0;

                foreach (var decision in request.Decisions)
                {
                    if (!reviewCandidates.TryGetValue(decision.CandidateId, out var candidate))
                    {
                        throw new CaseStateException("One or more extraction candidates are not pending or do not exist.");
                    }

                    var action = ParseExtractionDecisionAction(decision.Action);
                    var previousValue = candidate.CandidateValue;
                    var finalValue = previousValue;

                    switch (action)
                    {
                        case ExtractionDecisionAction.Approve:
                            candidate.Status = ExtractionCandidateStatus.Approved;
                            approvedValues[candidate.FieldKey] = finalValue;
                            break;
                        case ExtractionDecisionAction.Edit:
                            finalValue = decision.EditedValue!.Trim();
                            candidate.CandidateValue = finalValue;
                            candidate.Status = ExtractionCandidateStatus.Approved;
                            approvedValues[candidate.FieldKey] = finalValue;
                            break;
                        case ExtractionDecisionAction.Reject:
                            candidate.Status = ExtractionCandidateStatus.Rejected;
                            rejectedCount++;
                            break;
                    }

                    candidate.ReviewedByUserId = actorUserId;
                    candidate.ReviewedAt = reviewedAt;

                    decisionAuditItems.Add(
                        new
                        {
                            CandidateId = candidate.Id,
                            candidate.FieldKey,
                            SourceVersionNumber = candidate.SourceVersionNumber,
                            candidate.ConfidenceScore,
                            Action = action.ToString(),
                            PreviousValue = previousValue,
                            FinalValue = candidate.CandidateValue,
                            candidate.Status,
                            ReviewerUserId = actorUserId,
                            ReviewedAt = reviewedAt
                        });
                }

                if (approvedValues.Count > 0)
                {
                    caseEntity.IntakeData = ApplyApprovedCandidateValues(caseEntity.IntakeData, approvedValues);
                    caseEntity.UpdatedAt = reviewedAt;
                }

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseExtractionDecisionsReviewed",
                    new
                    {
                        CaseId = caseEntity.Id,
                        DocumentId = document.Id,
                        ReviewedByUserId = actorUserId,
                        ReviewedAt = reviewedAt,
                        TotalDecisions = request.Decisions.Count,
                        AppliedCount = approvedValues.Count,
                        RejectedCount = rejectedCount,
                        Decisions = decisionAuditItems
                    },
                    token,
                    caseEntity.Id);

                var orderedCandidates = request.Decisions
                    .Select(decision => reviewCandidates[decision.CandidateId])
                    .ToList();

                response = new ApplyExtractionDecisionsResponse
                {
                    CaseId = caseEntity.Id,
                    DocumentId = document.Id,
                    ReviewedAt = reviewedAt,
                    TotalDecisions = request.Decisions.Count,
                    AppliedCount = approvedValues.Count,
                    RejectedCount = rejectedCount,
                    Candidates = orderedCandidates.Select(MapExtractionCandidate).ToList()
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<GenerateCaseHandoffPacketResponse> GenerateCaseHandoffPacketAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "GenerateCaseHandoffPacket", cancellationToken);

        GenerateCaseHandoffPacketResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                EnsureCaseEligibleForHandoff(caseEntity);

                var steps = (await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, token)).ToList();
                if (steps.Count == 0)
                {
                    throw new CaseStateException("A generated case plan is required before a handoff packet can be created.");
                }

                var requiredActions = steps
                    .Where(step => step.Status is not (WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped))
                    .OrderBy(step => step.Sequence)
                    .Select(MapHandoffAction)
                    .ToList();

                if (requiredActions.Count == 0)
                {
                    throw new CaseStateException("At least one open required action is needed before creating a handoff packet.");
                }

                var documents = await _caseDocumentRepository.GetByCaseIdAsync(caseEntity.Id, token);
                if (documents.Count == 0)
                {
                    throw new CaseStateException("At least one case document is required before creating a handoff packet.");
                }

                var evidenceContext = documents
                    .Select(MapHandoffEvidence)
                    .ToList();

                var generatedAt = DateTime.UtcNow;
                var packetTitle = $"Advisor Handoff Packet - {caseEntity.CaseNumber}";
                var packetContent = BuildHandoffPacketContent(caseEntity, requiredActions, evidenceContext, generatedAt);
                var contentBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(packetContent));

                await WriteAuditEventAsync(
                    actorUserId,
                    "CaseHandoffPacketGenerated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        caseEntity.CaseNumber,
                        PacketTitle = packetTitle,
                        GeneratedByUserId = actorUserId,
                        GeneratedAt = generatedAt,
                        RequiredActionCount = requiredActions.Count,
                        EvidenceCount = evidenceContext.Count
                    },
                    token,
                    caseEntity.Id);

                response = new GenerateCaseHandoffPacketResponse
                {
                    CaseId = caseEntity.Id,
                    CaseNumber = caseEntity.CaseNumber,
                    PacketTitle = packetTitle,
                    ContentType = "text/plain",
                    ContentBase64 = contentBase64,
                    GeneratedAt = generatedAt,
                    RequiredActions = requiredActions,
                    EvidenceContext = evidenceContext
                };
            },
            cancellationToken);

        return response!;
    }

    public async Task<GenerateCasePlanResponse> RecalculateCasePlanReadinessAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "RecalculateCasePlanReadiness", cancellationToken);

        GenerateCasePlanResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                var steps = (await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, token)).ToList();
                if (steps.Count == 0)
                {
                    throw new CaseStateException("A generated case plan is required before readiness can be recalculated.");
                }

                var dependencies = await _workflowStepRepository.GetDependenciesByCaseIdAsync(caseEntity.Id, token);
                var nowUtc = DateTime.UtcNow;
                var changedSteps = RecalculateReadinessForNonOverriddenSteps(steps, dependencies, nowUtc);

                caseEntity.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CasePlanReadinessRecalculated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        RecalculatedAt = nowUtc,
                        ChangedStepCount = changedSteps.Count,
                        ChangedStepKeys = changedSteps
                    },
                    token,
                    caseEntity.Id);

                response = MapGeneratedCasePlan(caseEntity.Id, nowUtc, steps, dependencies);
            },
            cancellationToken);

        return response!;
    }

    public async Task<GenerateCasePlanResponse> OverrideWorkflowStepReadinessAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid stepId,
        OverrideWorkflowStepReadinessRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _overrideWorkflowStepReadinessValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Manager, actorUserId, caseId, "OverrideWorkflowStepReadiness", cancellationToken);

        GenerateCasePlanResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                var steps = (await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, token)).ToList();
                if (steps.Count == 0)
                {
                    throw new CaseStateException("A generated case plan is required before readiness can be overridden.");
                }

                var step = steps.FirstOrDefault(x => x.Id == stepId);
                if (step is null || step.CaseId != caseEntity.Id || step.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                if (step.Status is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped)
                {
                    throw new CaseStateException(
                        "Readiness override is not allowed for completed or skipped workflow steps.");
                }

                var targetStatus = ParseReadinessOverrideStatus(request.TargetStatus);
                var previousStatus = step.Status;
                var nowUtc = DateTime.UtcNow;

                step.Status = targetStatus;
                step.IsReadinessOverridden = true;
                step.ReadinessOverrideRationale = request.Rationale.Trim();
                step.ReadinessOverrideByUserId = actorUserId;
                step.ReadinessOverriddenAt = nowUtc;
                step.UpdatedAt = nowUtc;
                caseEntity.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "CasePlanReadinessOverridden",
                    new
                    {
                        CaseId = caseEntity.Id,
                        StepId = step.Id,
                        step.StepKey,
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = step.Status.ToString(),
                        Rationale = step.ReadinessOverrideRationale,
                        OverriddenByUserId = actorUserId,
                        OverriddenAt = nowUtc
                    },
                    token,
                    caseEntity.Id);

                var dependencies = await _workflowStepRepository.GetDependenciesByCaseIdAsync(caseEntity.Id, token);
                response = MapGeneratedCasePlan(caseEntity.Id, nowUtc, steps, dependencies);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseTaskWorkspaceResponse> GetCaseTaskWorkspaceAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Editor, actorUserId, caseId, "GetCaseTaskWorkspace", cancellationToken);

        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseId, cancellationToken);
        if (steps.Count == 0)
        {
            throw new CaseStateException("A generated case plan is required before opening the task workspace.");
        }

        var dependencies = await _workflowStepRepository.GetDependenciesByCaseIdAsync(caseId, cancellationToken);
        return MapCaseTaskWorkspace(caseId, DateTime.UtcNow, steps, dependencies);
    }

    public async Task<CaseTaskWorkspaceResponse> UpdateWorkflowTaskStatusAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid stepId,
        UpdateWorkflowTaskStatusRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _updateWorkflowTaskStatusValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        var caseForAccess = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseForAccess, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Editor, actorUserId, caseId, "UpdateWorkflowTaskStatus", cancellationToken);

        CaseTaskWorkspaceResponse? response = null;

        await _unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, token);
                var steps = (await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, token)).ToList();
                if (steps.Count == 0)
                {
                    throw new CaseStateException("A generated case plan is required before updating task status.");
                }

                var step = steps.FirstOrDefault(x => x.Id == stepId);
                if (step is null || step.CaseId != caseEntity.Id || step.TenantId != tenantId)
                {
                    throw new TenantAccessDeniedException();
                }

                if (step.Status is WorkflowStepStatus.Skipped)
                {
                    throw new CaseStateException("Skipped workflow steps cannot be updated.");
                }

                var targetStatus = ParseTaskExecutionStatus(request.TargetStatus);
                EnsureTaskExecutionTransitionAllowed(step.Status, targetStatus);

                var dependencies = await _workflowStepRepository.GetDependenciesByCaseIdAsync(caseEntity.Id, token);
                var stepById = steps.ToDictionary(x => x.Id, x => x);
                var dependencyMap = dependencies
                    .GroupBy(x => x.StepId)
                    .ToDictionary(x => x.Key, x => x.Select(y => y.DependsOnStepId).ToList());

                if (!step.IsReadinessOverridden
                    && !AreDependenciesSatisfied(step.Id, stepById, dependencyMap)
                    && targetStatus is WorkflowStepStatus.InProgress or WorkflowStepStatus.AwaitingEvidence or WorkflowStepStatus.Complete)
                {
                    throw new CaseStateException("Task status cannot advance while prerequisite steps are incomplete.");
                }

                var previousStatus = step.Status;
                var nowUtc = DateTime.UtcNow;
                step.Status = targetStatus;
                step.UpdatedAt = nowUtc;

                IReadOnlyList<string> changedReadinessSteps = Array.Empty<string>();
                if (targetStatus == WorkflowStepStatus.Complete)
                {
                    changedReadinessSteps = RecalculateReadinessForNonOverriddenSteps(steps, dependencies, nowUtc);
                }

                caseEntity.UpdatedAt = nowUtc;

                await WriteAuditEventAsync(
                    actorUserId,
                    "WorkflowTaskStatusUpdated",
                    new
                    {
                        CaseId = caseEntity.Id,
                        StepId = step.Id,
                        step.StepKey,
                        PreviousStatus = previousStatus.ToString(),
                        NewStatus = step.Status.ToString(),
                        ActorUserId = actorUserId,
                        Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                        UpdatedAt = nowUtc,
                        ChangedReadinessStepKeys = changedReadinessSteps
                    },
                    token,
                    caseEntity.Id);

                await WriteTaskNotificationsAsync(
                    actorUserId,
                    caseEntity,
                    step,
                    targetStatus,
                    string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                    nowUtc,
                    token);

                response = MapCaseTaskWorkspace(caseEntity.Id, nowUtc, steps, dependencies);
            },
            cancellationToken);

        return response!;
    }

    public async Task<CaseTimelineResponse> GetCaseTimelineAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken)
    {
        var caseEntity = await LoadCaseForTenantAsync(tenantId, caseId, cancellationToken);
        var effectiveRole = await ResolveEffectiveCaseRoleAsync(tenantId, actorUserId, caseEntity, cancellationToken);
        await EnsureCaseRoleAsync(effectiveRole, CaseRole.Reader, actorUserId, caseId, "GetCaseTimeline", cancellationToken);

        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseId, cancellationToken);
        var auditEvents = await _auditRepository.GetByCaseIdAsync(caseId, cancellationToken);
        var timelineEvents = auditEvents
            .Where(
                x => x.EventType is
                    "CaseCreated"
                    or "CaseStatusChanged"
                    or "CasePlanGenerated"
                    or "WorkflowTaskOwnershipInitialized"
                    or "WorkflowTaskStatusUpdated"
                    or "CaseNotificationQueued")
            .OrderBy(x => x.CreatedAt)
            .Select(MapTimelineEvent)
            .ToList();

        return new CaseTimelineResponse
        {
            CaseId = caseId,
            CurrentOwners = steps
                .OrderBy(x => x.Sequence)
                .Select(
                    x => new CaseTaskOwnerResponse
                    {
                        StepId = x.Id,
                        StepKey = x.StepKey,
                        AssignedUserId = x.AssignedUserId
                    })
                .ToList(),
            Events = timelineEvents
        };
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

    private static void EnsureCaseHasCompletedIntake(Case caseEntity)
    {
        if (string.IsNullOrWhiteSpace(caseEntity.IntakeData) || caseEntity.IntakeCompletedAt is null)
        {
            throw new CaseStateException(
                "Structured intake must be completed before generating a case plan.");
        }
    }

    private static IntakeSnapshot ParseIntakeSnapshot(string intakeData)
    {
        try
        {
            using var document = JsonDocument.Parse(intakeData);
            var root = document.RootElement;

            return new IntakeSnapshot(
                HasWill: ReadBooleanOrDefault(root, "HasWill"),
                RequiresLegalSupport: ReadBooleanOrDefault(root, "RequiresLegalSupport"),
                RequiresFinancialSupport: ReadBooleanOrDefault(root, "RequiresFinancialSupport"));
        }
        catch (JsonException)
        {
            throw new CaseStateException(
                "Stored intake data is invalid and cannot be used for plan generation.");
        }
    }

    private static bool ReadBooleanOrDefault(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => false
        };
    }

    private static IReadOnlyList<PlanStepBlueprint> BuildPlanBlueprints(IntakeSnapshot intakeSnapshot)
    {
        var blueprints = new List<PlanStepBlueprint>
        {
            new("collect-civil-records", "Collect civil and identity records"),
            new("gather-estate-inventory", "Gather estate and account inventory"),
            new(
                "submit-succession-notification",
                "Submit succession notification",
                "collect-civil-records",
                "gather-estate-inventory")
        };

        if (intakeSnapshot.HasWill)
        {
            blueprints.Add(
                new(
                    "validate-will",
                    "Validate will and testament requirements",
                    "collect-civil-records"));
        }

        if (intakeSnapshot.RequiresLegalSupport)
        {
            blueprints.Add(
                new(
                    "engage-legal-support",
                    "Engage legal support and share case context",
                    "submit-succession-notification"));
        }

        if (intakeSnapshot.RequiresFinancialSupport)
        {
            blueprints.Add(
                new(
                    "engage-financial-support",
                    "Engage financial support for account and tax obligations",
                    "gather-estate-inventory"));
        }

        return blueprints;
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

    private static void EnsureCaseEligibleForHandoff(Case caseEntity)
    {
        if (caseEntity.Status != CaseStatus.Active)
        {
            throw new CaseStateException("Handoff packets can only be generated for active cases.");
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

    private static CaseDocumentClassification ParseCaseDocumentClassification(string value)
    {
        if (Enum.TryParse<CaseDocumentClassification>(value.Trim(), ignoreCase: true, out var classification))
        {
            return classification;
        }

        throw new CaseStateException("The requested document classification is not recognized.");
    }

    private static string ParseOutboundTemplateKey(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "CASESUMMARYLETTER" => "CaseSummaryLetter",
            "REQUIREDDOCUMENTSCHECKLIST" => "RequiredDocumentsChecklist",
            _ => throw new CaseStateException("The requested outbound template is not recognized.")
        };
    }

    private static ExtractionDecisionAction ParseExtractionDecisionAction(string value)
    {
        if (Enum.TryParse<ExtractionDecisionAction>(value.Trim(), ignoreCase: true, out var action))
        {
            return action;
        }

        throw new CaseStateException("The requested extraction decision action is not recognized.");
    }

    private static IReadOnlyDictionary<string, string> BuildOutboundTemplateFields(string templateKey, Case caseEntity)
    {
        try
        {
            using var intakeDocument = JsonDocument.Parse(caseEntity.IntakeData!);
            var intake = intakeDocument.RootElement;

            var primaryContactName = ReadStringOrEmpty(intake, "PrimaryContactName");
            var relationshipToDeceased = ReadStringOrEmpty(intake, "RelationshipToDeceased");
            var hasWill = ReadBooleanOrDefault(intake, "HasWill");
            var requiresLegalSupport = ReadBooleanOrDefault(intake, "RequiresLegalSupport");
            var requiresFinancialSupport = ReadBooleanOrDefault(intake, "RequiresFinancialSupport");

            if (string.IsNullOrWhiteSpace(primaryContactName) || string.IsNullOrWhiteSpace(relationshipToDeceased))
            {
                throw new CaseStateException(
                    "Required intake fields are missing for outbound template generation.");
            }

            return templateKey switch
            {
                "CaseSummaryLetter" => new Dictionary<string, string>
                {
                    ["CaseNumber"] = caseEntity.CaseNumber,
                    ["DeceasedFullName"] = caseEntity.DeceasedFullName,
                    ["DateOfDeath"] = caseEntity.DateOfDeath.ToString("yyyy-MM-dd"),
                    ["PrimaryContactName"] = primaryContactName,
                    ["RelationshipToDeceased"] = relationshipToDeceased,
                    ["CaseType"] = caseEntity.CaseType,
                    ["Urgency"] = caseEntity.Urgency,
                    ["CaseStatus"] = caseEntity.Status.ToString()
                },
                "RequiredDocumentsChecklist" => new Dictionary<string, string>
                {
                    ["CaseNumber"] = caseEntity.CaseNumber,
                    ["DeceasedFullName"] = caseEntity.DeceasedFullName,
                    ["PrimaryContactName"] = primaryContactName,
                    ["HasWill"] = hasWill ? "Yes" : "No",
                    ["RequiresLegalSupport"] = requiresLegalSupport ? "Yes" : "No",
                    ["RequiresFinancialSupport"] = requiresFinancialSupport ? "Yes" : "No"
                },
                _ => throw new CaseStateException("The requested outbound template is not recognized.")
            };
        }
        catch (JsonException)
        {
            throw new CaseStateException("Stored intake data is invalid and cannot be used for template generation.");
        }
    }

    private static string BuildOutboundTemplateContent(string templateKey, IReadOnlyDictionary<string, string> mappedFields)
    {
        var lines = new List<string>
        {
            "Sanzu Outbound Template",
            $"Template: {templateKey}",
            string.Empty
        };

        foreach (var field in mappedFields)
        {
            lines.Add($"{field.Key}: {field.Value}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string BuildHandoffPacketContent(
        Case caseEntity,
        IReadOnlyList<CaseHandoffActionItemResponse> requiredActions,
        IReadOnlyList<CaseHandoffEvidenceItemResponse> evidenceContext,
        DateTime generatedAt)
    {
        var lines = new List<string>
        {
            "Sanzu Advisor Handoff Packet",
            $"GeneratedAt: {generatedAt:O}",
            $"CaseNumber: {caseEntity.CaseNumber}",
            $"CaseId: {caseEntity.Id}",
            $"DeceasedFullName: {caseEntity.DeceasedFullName}",
            $"DateOfDeath: {caseEntity.DateOfDeath:yyyy-MM-dd}",
            $"CaseType: {caseEntity.CaseType}",
            $"Urgency: {caseEntity.Urgency}",
            $"CaseStatus: {caseEntity.Status}",
            string.Empty,
            "Required Actions:"
        };

        foreach (var action in requiredActions)
        {
            lines.Add(
                $"- [{action.Sequence}] {action.Title} ({action.StepKey}) | Status: {action.Status} | DueDate: {action.DueDate:yyyy-MM-dd} | AssignedUserId: {action.AssignedUserId}");
        }

        lines.Add(string.Empty);
        lines.Add("Evidence Context:");

        foreach (var evidence in evidenceContext)
        {
            lines.Add(
                $"- {evidence.FileName} | Type: {evidence.ContentType} | SizeBytes: {evidence.SizeBytes} | Version: {evidence.VersionNumber} | Classification: {evidence.Classification} | UploadedAt: {evidence.UploadedAt:O}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool IsSupportedExtractionContentType(string contentType)
    {
        var normalized = contentType.Trim().ToLowerInvariant();
        return normalized.StartsWith("text/")
               || normalized is "application/json";
    }

    private static string ApplyApprovedCandidateValues(
        string? existingIntakeData,
        IReadOnlyDictionary<string, string> approvedValues)
    {
        JsonObject root;
        if (string.IsNullOrWhiteSpace(existingIntakeData))
        {
            root = [];
        }
        else
        {
            try
            {
                root = JsonNode.Parse(existingIntakeData) as JsonObject
                    ?? throw new CaseStateException("Stored intake data is invalid and cannot be updated from extraction review.");
            }
            catch (JsonException)
            {
                throw new CaseStateException("Stored intake data is invalid and cannot be updated from extraction review.");
            }
        }

        foreach (var pair in approvedValues)
        {
            root[pair.Key] = pair.Value;
        }

        return root.ToJsonString();
    }

    private static List<ExtractionCandidate> BuildExtractionCandidates(
        Guid tenantId,
        Guid caseId,
        CaseDocument document,
        string contentText,
        DateTime extractedAt)
    {
        var candidates = new List<ExtractionCandidate>();

        void AddCandidate(string fieldKey, string value, decimal confidenceScore)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            candidates.Add(
                new ExtractionCandidate
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    CaseId = caseId,
                    DocumentId = document.Id,
                    FieldKey = fieldKey,
                    CandidateValue = value.Trim(),
                    ConfidenceScore = confidenceScore,
                    SourceVersionNumber = document.CurrentVersionNumber,
                    Status = ExtractionCandidateStatus.Pending,
                    CreatedAt = extractedAt
                });
        }

        AddCandidate("PrimaryContactName", TryExtractLabelValue(contentText, "PrimaryContactName"), 0.95m);
        AddCandidate("PrimaryContactPhone", TryExtractLabelValue(contentText, "PrimaryContactPhone"), 0.95m);
        AddCandidate("RelationshipToDeceased", TryExtractLabelValue(contentText, "RelationshipToDeceased"), 0.95m);
        AddCandidate("DeceasedFullName", TryExtractLabelValue(contentText, "DeceasedFullName"), 0.95m);
        AddCandidate("DateOfDeath", TryExtractLabelValue(contentText, "DateOfDeath"), 0.9m);

        if (candidates.Count == 0)
        {
            var snippet = contentText.Length <= 200
                ? contentText
                : contentText[..200];

            AddCandidate("RawSnippet", snippet, 0.4m);
        }

        return candidates;
    }

    private static string TryExtractLabelValue(string contentText, string fieldKey)
    {
        var pattern = $@"(?im)^\s*{Regex.Escape(fieldKey)}\s*[:=]\s*(?<value>.+?)\s*$";
        var match = Regex.Match(contentText, pattern, RegexOptions.CultureInvariant);
        return match.Success
            ? match.Groups["value"].Value.Trim()
            : string.Empty;
    }

    private static string ReadStringOrEmpty(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return value.GetString()?.Trim() ?? string.Empty;
    }

    private static byte[] DecodeDocumentContent(string contentBase64)
    {
        try
        {
            var contentBytes = Convert.FromBase64String(contentBase64.Trim());
            if (contentBytes.Length == 0)
            {
                throw new ValidationException(
                    new[]
                    {
                        new FluentValidation.Results.ValidationFailure(
                            nameof(UploadCaseDocumentRequest.ContentBase64),
                            "ContentBase64 must contain document bytes.")
                    });
            }

            return contentBytes;
        }
        catch (FormatException)
        {
            throw new ValidationException(
                new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(UploadCaseDocumentRequest.ContentBase64),
                        "ContentBase64 must be valid Base64 data.")
                });
        }
    }

    private static WorkflowStepStatus ParseReadinessOverrideStatus(string value)
    {
        if (Enum.TryParse<WorkflowStepStatus>(value.Trim(), ignoreCase: true, out var status)
            && status is WorkflowStepStatus.Ready or WorkflowStepStatus.Blocked)
        {
            return status;
        }

        throw new CaseStateException("Readiness overrides only support Ready or Blocked statuses.");
    }

    private static WorkflowStepStatus ParseTaskExecutionStatus(string value)
    {
        return value.Trim().ToUpperInvariant() switch
        {
            "STARTED" => WorkflowStepStatus.InProgress,
            "COMPLETED" => WorkflowStepStatus.Complete,
            "NEEDSREVIEW" => WorkflowStepStatus.AwaitingEvidence,
            _ => throw new CaseStateException("Task status updates only support Started, Completed, or NeedsReview.")
        };
    }

    private static void EnsureTaskExecutionTransitionAllowed(WorkflowStepStatus currentStatus, WorkflowStepStatus targetStatus)
    {
        if (currentStatus == targetStatus)
        {
            return;
        }

        if (currentStatus is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped)
        {
            throw new CaseStateException("Completed or skipped tasks cannot be moved to another status.");
        }

        var isAllowed = targetStatus switch
        {
            WorkflowStepStatus.InProgress => currentStatus is WorkflowStepStatus.Ready or WorkflowStepStatus.Overdue or WorkflowStepStatus.AwaitingEvidence,
            WorkflowStepStatus.AwaitingEvidence => currentStatus is WorkflowStepStatus.Ready or WorkflowStepStatus.Overdue or WorkflowStepStatus.InProgress,
            WorkflowStepStatus.Complete => currentStatus is WorkflowStepStatus.Ready or WorkflowStepStatus.Overdue or WorkflowStepStatus.InProgress or WorkflowStepStatus.AwaitingEvidence,
            _ => false
        };

        if (!isAllowed)
        {
            throw new CaseStateException($"Invalid task transition from {currentStatus} to {targetStatus}.");
        }
    }

    private static DateTime BuildStepDueDate(DateTime planGeneratedAtUtc, int sequence)
    {
        var offsetDays = 2 + (sequence * 2);
        return planGeneratedAtUtc.Date.AddDays(offsetDays);
    }

    private static string BuildDeadlineSource(int sequence)
    {
        var offsetDays = 2 + (sequence * 2);
        return $"Rule: target completion within {offsetDays} days from plan generation.";
    }

    private static IReadOnlyList<string> RecalculateReadinessForNonOverriddenSteps(
        IReadOnlyCollection<WorkflowStepInstance> steps,
        IReadOnlyList<WorkflowStepDependency> dependencies,
        DateTime nowUtc)
    {
        var changedSteps = new List<string>();
        var stepById = steps.ToDictionary(x => x.Id, x => x);
        var dependencyMap = dependencies
            .GroupBy(x => x.StepId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.DependsOnStepId).ToList());

        foreach (var step in steps)
        {
            if (step.IsReadinessOverridden)
            {
                continue;
            }

            if (step.Status is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped)
            {
                continue;
            }

            var shouldBeReady = AreDependenciesSatisfied(step.Id, stepById, dependencyMap);
            var desiredStatus = shouldBeReady ? WorkflowStepStatus.Ready : WorkflowStepStatus.Blocked;
            if (step.Status == desiredStatus)
            {
                continue;
            }

            step.Status = desiredStatus;
            step.UpdatedAt = nowUtc;
            changedSteps.Add(step.StepKey);
        }

        return changedSteps;
    }

    private static bool AreDependenciesSatisfied(
        Guid stepId,
        IReadOnlyDictionary<Guid, WorkflowStepInstance> stepById,
        IReadOnlyDictionary<Guid, List<Guid>> dependencyMap)
    {
        var dependencyIds = dependencyMap.TryGetValue(stepId, out var dependsOn)
            ? dependsOn
            : new List<Guid>();

        return dependencyIds.Count == 0
            || dependencyIds.All(
                dependencyId =>
                    stepById.TryGetValue(dependencyId, out var dependencyStep)
                    && dependencyStep.Status is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped);
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

    private static CaseDocumentUploadResponse MapDocumentUpload(CaseDocument document)
    {
        return new CaseDocumentUploadResponse
        {
            DocumentId = document.Id,
            CaseId = document.CaseId,
            VersionNumber = document.CurrentVersionNumber,
            Classification = document.Classification.ToString(),
            FileName = document.FileName,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            UploadedByUserId = document.UploadedByUserId,
            UploadedAt = document.UpdatedAt
        };
    }

    private static CaseDocumentDownloadResponse MapDocumentDownload(CaseDocument document)
    {
        return new CaseDocumentDownloadResponse
        {
            DocumentId = document.Id,
            CaseId = document.CaseId,
            VersionNumber = document.CurrentVersionNumber,
            Classification = document.Classification.ToString(),
            FileName = document.FileName,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            ContentBase64 = Convert.ToBase64String(document.Content),
            UploadedAt = document.CreatedAt
        };
    }

    private static CaseDocumentVersionHistoryResponse MapDocumentVersionHistory(
        CaseDocument document,
        IReadOnlyList<CaseDocumentVersion> versions)
    {
        return new CaseDocumentVersionHistoryResponse
        {
            DocumentId = document.Id,
            CaseId = document.CaseId,
            Classification = document.Classification.ToString(),
            LatestVersionNumber = document.CurrentVersionNumber,
            Versions = versions.Select(MapDocumentVersion).ToList()
        };
    }

    private static CaseDocumentVersionResponse MapDocumentVersion(CaseDocumentVersion version)
    {
        return new CaseDocumentVersionResponse
        {
            VersionId = version.Id,
            DocumentId = version.DocumentId,
            VersionNumber = version.VersionNumber,
            FileName = version.FileName,
            ContentType = version.ContentType,
            SizeBytes = version.SizeBytes,
            UploadedByUserId = version.UploadedByUserId,
            UploadedAt = version.CreatedAt
        };
    }

    private static CaseDocumentClassificationResponse MapDocumentClassification(CaseDocument document)
    {
        return new CaseDocumentClassificationResponse
        {
            DocumentId = document.Id,
            CaseId = document.CaseId,
            Classification = document.Classification.ToString(),
            UpdatedAt = document.UpdatedAt
        };
    }

    private static ExtractionCandidateResponse MapExtractionCandidate(ExtractionCandidate candidate)
    {
        return new ExtractionCandidateResponse
        {
            CandidateId = candidate.Id,
            FieldKey = candidate.FieldKey,
            CandidateValue = candidate.CandidateValue,
            ConfidenceScore = candidate.ConfidenceScore,
            SourceVersionNumber = candidate.SourceVersionNumber,
            Status = candidate.Status.ToString()
        };
    }

    private static CaseHandoffActionItemResponse MapHandoffAction(WorkflowStepInstance step)
    {
        return new CaseHandoffActionItemResponse
        {
            StepId = step.Id,
            StepKey = step.StepKey,
            Title = step.Title,
            Status = step.Status.ToString(),
            Sequence = step.Sequence,
            DueDate = step.DueDate,
            AssignedUserId = step.AssignedUserId
        };
    }

    private static CaseHandoffEvidenceItemResponse MapHandoffEvidence(CaseDocument document)
    {
        return new CaseHandoffEvidenceItemResponse
        {
            DocumentId = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            VersionNumber = document.CurrentVersionNumber,
            Classification = document.Classification.ToString(),
            UploadedAt = document.UpdatedAt
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

    private static GenerateCasePlanResponse MapGeneratedCasePlan(
        Guid caseId,
        DateTime generatedAt,
        IEnumerable<WorkflowStepInstance> steps,
        IReadOnlyList<WorkflowStepDependency> dependencies)
    {
        var dependencyMap = dependencies
            .GroupBy(x => x.StepId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Guid>)x.Select(y => y.DependsOnStepId).ToList());

        return new GenerateCasePlanResponse
        {
            CaseId = caseId,
            GeneratedAt = generatedAt,
            Steps = steps
                .OrderBy(x => x.Sequence)
                .Select(
                    step => new CasePlanStepResponse
                    {
                        StepId = step.Id,
                        StepKey = step.StepKey,
                        Title = step.Title,
                        Sequence = step.Sequence,
                        Status = step.Status,
                        DependsOnStepIds = dependencyMap.TryGetValue(step.Id, out var dependsOn)
                            ? dependsOn
                            : Array.Empty<Guid>()
                    })
                .ToList()
        };
    }

    private static CaseTaskWorkspaceResponse MapCaseTaskWorkspace(
        Guid caseId,
        DateTime retrievedAt,
        IEnumerable<WorkflowStepInstance> steps,
        IReadOnlyList<WorkflowStepDependency> dependencies)
    {
        var dependencyMap = dependencies
            .GroupBy(x => x.StepId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Guid>)x.Select(y => y.DependsOnStepId).ToList());

        var tasks = steps
            .Select(
                step =>
                {
                    var urgency = GetUrgencyIndicator(step, retrievedAt);
                    return new CaseTaskItemResponse
                    {
                        StepId = step.Id,
                        StepKey = step.StepKey,
                        Title = step.Title,
                        Sequence = step.Sequence,
                        PriorityRank = GetTaskPriorityRank(step),
                        Status = step.Status,
                        AssignedUserId = step.AssignedUserId,
                        DueDate = step.DueDate,
                        DeadlineSource = step.DeadlineSource,
                        UrgencyIndicator = urgency,
                        DependsOnStepIds = dependencyMap.TryGetValue(step.Id, out var dependsOn)
                            ? dependsOn
                            : Array.Empty<Guid>()
                    };
                })
            .OrderBy(x => x.PriorityRank)
            .ThenBy(
                x => x.UrgencyIndicator switch
                {
                    "overdue" => 0,
                    "due-soon" => 1,
                    "upcoming" => 2,
                    _ => 3
                })
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Sequence)
            .ToList();

        return new CaseTaskWorkspaceResponse
        {
            CaseId = caseId,
            RetrievedAt = retrievedAt,
            Tasks = tasks
        };
    }

    private static int GetTaskPriorityRank(WorkflowStepInstance step)
    {
        return step.Status switch
        {
            WorkflowStepStatus.InProgress => 1,
            WorkflowStepStatus.Ready or WorkflowStepStatus.Overdue => 2,
            WorkflowStepStatus.AwaitingEvidence => 3,
            WorkflowStepStatus.Blocked => 4,
            WorkflowStepStatus.NotStarted => 5,
            WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped => 6,
            _ => 7
        };
    }

    private static string GetUrgencyIndicator(WorkflowStepInstance step, DateTime nowUtc)
    {
        if (step.DueDate is null || step.Status is WorkflowStepStatus.Complete or WorkflowStepStatus.Skipped)
        {
            return "none";
        }

        var dueDate = step.DueDate.Value.Date;
        var today = nowUtc.Date;
        if (dueDate < today)
        {
            return "overdue";
        }

        var daysRemaining = (dueDate - today).TotalDays;
        if (daysRemaining <= 2)
        {
            return "due-soon";
        }

        if (daysRemaining <= 7)
        {
            return "upcoming";
        }

        return "none";
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

    private static CaseTimelineEventResponse MapTimelineEvent(AuditEvent auditEvent)
    {
        return new CaseTimelineEventResponse
        {
            EventType = auditEvent.EventType,
            Description = BuildTimelineDescription(auditEvent),
            ActorUserId = auditEvent.ActorUserId,
            OccurredAt = auditEvent.CreatedAt
        };
    }

    private static string BuildTimelineDescription(AuditEvent auditEvent)
    {
        try
        {
            using var document = JsonDocument.Parse(auditEvent.Metadata);
            var root = document.RootElement;

            if (auditEvent.EventType == "WorkflowTaskStatusUpdated")
            {
                var stepKey = root.TryGetProperty("StepKey", out var stepKeyValue)
                    ? stepKeyValue.GetString()
                    : "task";
                var previous = root.TryGetProperty("PreviousStatus", out var previousValue)
                    ? previousValue.GetString()
                    : "Unknown";
                var current = root.TryGetProperty("NewStatus", out var currentValue)
                    ? currentValue.GetString()
                    : "Unknown";
                return $"Task {stepKey} moved from {previous} to {current}";
            }

            if (auditEvent.EventType == "WorkflowTaskOwnershipInitialized")
            {
                var ownerUserId = root.TryGetProperty("DefaultOwnerUserId", out var owner)
                    ? owner.GetString()
                    : null;
                return $"Task ownership initialized with default owner {ownerUserId}";
            }

            if (auditEvent.EventType == "CaseNotificationQueued")
            {
                var notificationType = root.TryGetProperty("NotificationType", out var notificationTypeValue)
                    ? notificationTypeValue.GetString()
                    : "Notification";
                var recipientCount = root.TryGetProperty("RecipientUserIds", out var recipientIds)
                    && recipientIds.ValueKind == JsonValueKind.Array
                    ? recipientIds.GetArrayLength()
                    : 0;
                return $"{notificationType} notification queued for {recipientCount} recipients";
            }

            if (auditEvent.EventType == "CaseHandoffPacketGenerated")
            {
                var actionCount = root.TryGetProperty("RequiredActionCount", out var actionCountValue)
                    && actionCountValue.ValueKind == JsonValueKind.Number
                    ? actionCountValue.GetInt32()
                    : 0;
                var evidenceCount = root.TryGetProperty("EvidenceCount", out var evidenceCountValue)
                    && evidenceCountValue.ValueKind == JsonValueKind.Number
                    ? evidenceCountValue.GetInt32()
                    : 0;
                return $"Advisor handoff packet generated with {actionCount} actions and {evidenceCount} evidence items";
            }
        }
        catch (JsonException)
        {
        }

        return BuildMilestoneDescription(auditEvent);
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

    private async Task WriteTaskNotificationsAsync(
        Guid actorUserId,
        Case caseEntity,
        WorkflowStepInstance step,
        WorkflowStepStatus targetStatus,
        string? notes,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var recipientUserIds = new HashSet<Guid> { caseEntity.ManagerUserId };
        if (step.AssignedUserId.HasValue)
        {
            recipientUserIds.Add(step.AssignedUserId.Value);
        }

        var acceptedParticipants = await _caseParticipantRepository.GetAcceptedByCaseIdAsync(caseEntity.Id, cancellationToken);
        foreach (var participant in acceptedParticipants)
        {
            if (participant.ParticipantUserId.HasValue)
            {
                recipientUserIds.Add(participant.ParticipantUserId.Value);
            }
        }

        var recipients = recipientUserIds.ToArray();
        if (recipients.Length == 0)
        {
            return;
        }

        await WriteAuditEventAsync(
            actorUserId,
            "CaseNotificationQueued",
            new
            {
                CaseId = caseEntity.Id,
                TaskId = step.Id,
                step.StepKey,
                NotificationType = "TaskStateChanged",
                TargetStatus = targetStatus.ToString(),
                RecipientUserIds = recipients,
                CreatedAt = nowUtc
            },
            cancellationToken,
            caseEntity.Id);

        if (targetStatus == WorkflowStepStatus.AwaitingEvidence)
        {
            await WriteAuditEventAsync(
                actorUserId,
                "CaseNotificationQueued",
                new
                {
                    CaseId = caseEntity.Id,
                    TaskId = step.Id,
                    step.StepKey,
                    NotificationType = "MissingInputRequired",
                    Notes = notes,
                    RecipientUserIds = recipients,
                    CreatedAt = nowUtc
                },
                cancellationToken,
                caseEntity.Id);
        }
    }

    private sealed record IntakeSnapshot(
        bool HasWill,
        bool RequiresLegalSupport,
        bool RequiresFinancialSupport);

    private sealed class PlanStepBlueprint
    {
        public PlanStepBlueprint(string stepKey, string title, params string[] dependsOnKeys)
        {
            StepKey = stepKey;
            Title = title;
            DependsOnKeys = dependsOnKeys;
        }

        public string StepKey { get; }
        public string Title { get; }
        public IReadOnlyList<string> DependsOnKeys { get; }
    }
}
