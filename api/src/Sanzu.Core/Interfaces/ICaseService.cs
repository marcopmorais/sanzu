using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

public interface ICaseService
{
    Task<CreateCaseResponse> CreateCaseAsync(
        Guid tenantId,
        Guid actorUserId,
        CreateCaseRequest request,
        CancellationToken cancellationToken);

    Task<CaseDetailsResponse> UpdateCaseDetailsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UpdateCaseDetailsRequest request,
        CancellationToken cancellationToken);

    Task<CaseDetailsResponse> UpdateCaseLifecycleAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UpdateCaseLifecycleRequest request,
        CancellationToken cancellationToken);

    Task<CaseMilestonesResponse> GetCaseMilestonesAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);

    Task<InviteCaseParticipantResponse> InviteCaseParticipantAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        InviteCaseParticipantRequest request,
        CancellationToken cancellationToken);

    Task<CaseParticipantResponse> AcceptCaseParticipantInvitationAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid participantId,
        AcceptCaseParticipantInvitationRequest request,
        CancellationToken cancellationToken);

    Task<CaseParticipantResponse> UpdateCaseParticipantRoleAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid participantId,
        UpdateCaseParticipantRoleRequest request,
        CancellationToken cancellationToken);

    Task<CaseDetailsResponse> GetCaseDetailsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);

    Task<CaseDetailsResponse> SubmitCaseIntakeAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        SubmitCaseIntakeRequest request,
        CancellationToken cancellationToken);

    Task<GenerateCasePlanResponse> GenerateCasePlanAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);

    Task<CaseDocumentUploadResponse> UploadCaseDocumentAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        UploadCaseDocumentRequest request,
        CancellationToken cancellationToken);

    Task<CaseDocumentDownloadResponse> DownloadCaseDocumentAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken);

    Task<CaseDocumentUploadResponse> UploadCaseDocumentVersionAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        UploadCaseDocumentRequest request,
        CancellationToken cancellationToken);

    Task<CaseDocumentVersionHistoryResponse> GetCaseDocumentVersionsAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        CancellationToken cancellationToken);

    Task<CaseDocumentClassificationResponse> UpdateCaseDocumentClassificationAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid documentId,
        UpdateCaseDocumentClassificationRequest request,
        CancellationToken cancellationToken);

    Task<GenerateOutboundTemplateResponse> GenerateOutboundTemplateAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        GenerateOutboundTemplateRequest request,
        CancellationToken cancellationToken);

    Task<GenerateCasePlanResponse> RecalculateCasePlanReadinessAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);

    Task<GenerateCasePlanResponse> OverrideWorkflowStepReadinessAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid stepId,
        OverrideWorkflowStepReadinessRequest request,
        CancellationToken cancellationToken);

    Task<CaseTaskWorkspaceResponse> GetCaseTaskWorkspaceAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);

    Task<CaseTaskWorkspaceResponse> UpdateWorkflowTaskStatusAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        Guid stepId,
        UpdateWorkflowTaskStatusRequest request,
        CancellationToken cancellationToken);

    Task<CaseTimelineResponse> GetCaseTimelineAsync(
        Guid tenantId,
        Guid actorUserId,
        Guid caseId,
        CancellationToken cancellationToken);
}
