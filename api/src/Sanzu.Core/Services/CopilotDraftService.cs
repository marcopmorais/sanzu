using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class CopilotDraftService : ICopilotDraftService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly ICaseHandoffRepository _caseHandoffRepository;
    private readonly ICaseDocumentRepository _caseDocumentRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly Dictionary<string, CopilotDraftType> DraftTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["evidence_request"] = CopilotDraftType.EvidenceRequest,
        ["handoff_checklist"] = CopilotDraftType.HandoffChecklist,
        ["recovery_plan"] = CopilotDraftType.RecoveryPlan,
        ["explain_why"] = CopilotDraftType.ExplainWhy
    };

    public CopilotDraftService(
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        ICaseHandoffRepository caseHandoffRepository,
        ICaseDocumentRepository caseDocumentRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _caseHandoffRepository = caseHandoffRepository;
        _caseDocumentRepository = caseDocumentRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CopilotDraftResponse> GenerateDraftAsync(
        Guid actorUserId, Guid tenantId, RequestCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        if (!DraftTypeMap.TryGetValue(request.DraftType, out var draftType))
            throw new CaseStateException($"Unknown draft type: {request.DraftType}");

        var hasRole = await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken);
        if (!hasRole)
            throw new TenantAccessDeniedException();

        var caseEntity = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
                         ?? throw new CaseStateException("Case not found.");

        var response = draftType switch
        {
            CopilotDraftType.EvidenceRequest => await BuildEvidenceRequestDraft(caseEntity, request, cancellationToken),
            CopilotDraftType.HandoffChecklist => await BuildHandoffChecklistDraft(caseEntity, request, cancellationToken),
            CopilotDraftType.RecoveryPlan => await BuildRecoveryPlanDraft(caseEntity, request, cancellationToken),
            CopilotDraftType.ExplainWhy => await BuildExplainWhyDraft(caseEntity, request, cancellationToken),
            _ => throw new CaseStateException($"Unsupported draft type: {draftType}")
        };

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                CaseId = caseEntity.Id,
                ActorUserId = actorUserId,
                EventType = "CopilotDraftRequested",
                Metadata = $"{{\"draftType\":\"{request.DraftType}\",\"draftId\":\"{response.Id}\"}}",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);

        return response;
    }

    public async Task<CopilotDraftAcceptedResponse> AcceptDraftAsync(
        Guid actorUserId, Guid tenantId, AcceptCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        var hasRole = await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken);
        if (!hasRole)
            throw new TenantAccessDeniedException();

        var now = DateTime.UtcNow;
        var editSeverity = string.IsNullOrWhiteSpace(request.EditedContent) ? "none" : "minor";

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                CaseId = null,
                ActorUserId = actorUserId,
                EventType = "CopilotDraftAccepted",
                Metadata = $"{{\"draftId\":\"{request.DraftId}\",\"editSeverity\":\"{editSeverity}\"}}",
                CreatedAt = now
            }, ct);
        }, cancellationToken);

        return new CopilotDraftAcceptedResponse
        {
            DraftId = request.DraftId,
            Status = "Accepted",
            AcceptedAt = now
        };
    }

    private async Task<CopilotDraftResponse> BuildEvidenceRequestDraft(
        Case caseEntity, RequestCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
        var documents = await _caseDocumentRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);

        var blockedSteps = steps.Where(s => s.Status == WorkflowStepStatus.Blocked || s.Status == WorkflowStepStatus.AwaitingEvidence).ToList();
        var reasonCategory = blockedSteps.FirstOrDefault()?.BlockedReasonCode?.ToString() ?? "EvidenceMissing";

        var checklist = new List<string>();
        var missingItems = new List<string>();

        foreach (var step in blockedSteps)
        {
            checklist.Add($"Provide evidence for: {step.Title}");
            if (!string.IsNullOrEmpty(step.BlockedReasonDetail))
                missingItems.Add(step.BlockedReasonDetail);
        }

        if (checklist.Count == 0)
        {
            checklist.Add("Review case documents for completeness");
            checklist.Add("Confirm all required fields are provided");
        }

        var confidence = blockedSteps.Count > 0 ? ConfidenceBand.High : ConfidenceBand.Medium;

        var content = $"Dear family,\n\nWe are writing regarding case {caseEntity.CaseNumber} for {caseEntity.DeceasedFullName}.\n\n"
                      + "To continue processing, we need the following:\n\n"
                      + string.Join("\n", checklist.Select((c, i) => $"{i + 1}. {c}"))
                      + "\n\nPlease provide these at your earliest convenience. If you have questions, contact your case manager.\n\nBest regards";

        return new CopilotDraftResponse
        {
            Id = Guid.NewGuid(),
            DraftType = "evidence_request",
            Content = content,
            Checklist = checklist,
            Explainability = new CopilotExplainabilityBlock
            {
                BasedOn = $"{blockedSteps.Count} blocked or awaiting-evidence workflow step(s) in case {caseEntity.CaseNumber}",
                ReasonCategory = reasonCategory,
                ConfidenceBand = confidence.ToString().ToLowerInvariant(),
                MissingOrUnknown = missingItems.Count > 0 ? missingItems : new List<string> { "Specific missing items could not be determined from step metadata" },
                SafeFallback = "Ask the family to contact the case manager for clarification."
            },
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<CopilotDraftResponse> BuildHandoffChecklistDraft(
        Case caseEntity, RequestCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        var handoff = request.HandoffId.HasValue
            ? await _caseHandoffRepository.GetByIdAsync(request.HandoffId.Value, cancellationToken)
            : await _caseHandoffRepository.GetLatestByCaseIdAsync(caseEntity.Id, cancellationToken);

        var documents = await _caseDocumentRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);

        var packetTitle = handoff?.PacketTitle ?? "External handoff";
        var completedSteps = steps.Count(s => s.Status == WorkflowStepStatus.Complete);
        var totalSteps = steps.Count;

        var checklist = new List<string>
        {
            $"Handoff packet: {packetTitle}",
            $"Case documents attached: {documents.Count}",
            $"Workflow progress: {completedSteps}/{totalSteps} steps complete",
            "Verify all required signatures are collected",
            "Confirm external partner contact details",
            "Include case summary and timeline"
        };

        var missingItems = new List<string>();
        var incompleteSteps = steps.Where(s => s.Status != WorkflowStepStatus.Complete && s.Status != WorkflowStepStatus.Skipped).ToList();
        if (incompleteSteps.Count > 0)
            missingItems.Add($"{incompleteSteps.Count} workflow step(s) not yet complete");
        if (documents.Count == 0)
            missingItems.Add("No documents attached to case");

        var confidence = (incompleteSteps.Count == 0 && documents.Count > 0) ? ConfidenceBand.High : ConfidenceBand.Medium;
        var reasonCategory = handoff != null ? "ExternalDependency" : "EvidenceMissing";

        var content = $"Handoff Checklist for Case {caseEntity.CaseNumber}\n"
                      + $"Deceased: {caseEntity.DeceasedFullName}\n"
                      + $"Packet: {packetTitle}\n\n"
                      + string.Join("\n", checklist.Select((c, i) => $"[ ] {c}"))
                      + "\n\nPlease review each item before sending the handoff packet.";

        return new CopilotDraftResponse
        {
            Id = Guid.NewGuid(),
            DraftType = "handoff_checklist",
            Content = content,
            Checklist = checklist,
            Explainability = new CopilotExplainabilityBlock
            {
                BasedOn = $"Case {caseEntity.CaseNumber} handoff state and {documents.Count} document(s)",
                ReasonCategory = reasonCategory,
                ConfidenceBand = confidence.ToString().ToLowerInvariant(),
                MissingOrUnknown = missingItems.Count > 0 ? missingItems : new List<string> { "All prerequisites appear to be met" },
                SafeFallback = "Escalate to case manager if any checklist item cannot be confirmed."
            },
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<CopilotDraftResponse> BuildRecoveryPlanDraft(
        Case caseEntity, RequestCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
        var blockedSteps = steps.Where(s => s.Status == WorkflowStepStatus.Blocked).ToList();

        WorkflowStepInstance? targetStep = null;
        if (request.WorkflowStepId.HasValue)
            targetStep = steps.FirstOrDefault(s => s.Id == request.WorkflowStepId.Value);
        targetStep ??= blockedSteps.FirstOrDefault();

        var reasonCode = targetStep?.BlockedReasonCode ?? BlockedReasonCode.EvidenceMissing;
        var reasonDetail = targetStep?.BlockedReasonDetail ?? "No additional detail available.";

        var (planSteps, fallback) = BuildRecoverySteps(reasonCode);

        var checklist = planSteps;
        var content = $"Recovery Plan for Case {caseEntity.CaseNumber}\n"
                      + $"Blocked step: {targetStep?.Title ?? "Unknown"}\n"
                      + $"Reason: {GetReasonLabel(reasonCode)}\n\n"
                      + "Steps to resolve:\n"
                      + string.Join("\n", planSteps.Select((s, i) => $"{i + 1}. {s}"))
                      + $"\n\nIf these steps do not resolve the issue: {fallback}";

        return new CopilotDraftResponse
        {
            Id = Guid.NewGuid(),
            DraftType = "recovery_plan",
            Content = content,
            Checklist = checklist,
            Explainability = new CopilotExplainabilityBlock
            {
                BasedOn = $"Blocked step \"{targetStep?.Title ?? "unknown"}\" with reason {reasonCode}",
                ReasonCategory = reasonCode.ToString(),
                ConfidenceBand = (targetStep != null ? ConfidenceBand.High : ConfidenceBand.Low).ToString().ToLowerInvariant(),
                MissingOrUnknown = string.IsNullOrEmpty(reasonDetail) || reasonDetail == "No additional detail available."
                    ? new List<string> { "Specific blocked reason detail is not available" }
                    : new List<string>(),
                SafeFallback = fallback
            },
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task<CopilotDraftResponse> BuildExplainWhyDraft(
        Case caseEntity, RequestCopilotDraftRequest request, CancellationToken cancellationToken)
    {
        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
        var blockedSteps = steps.Where(s => s.Status == WorkflowStepStatus.Blocked).ToList();

        var reasonGroups = blockedSteps
            .GroupBy(s => s.BlockedReasonCode ?? BlockedReasonCode.EvidenceMissing)
            .Select(g => new { Reason = g.Key, Count = g.Count(), Steps = g.ToList() })
            .OrderByDescending(g => g.Count)
            .ToList();

        var primaryReason = reasonGroups.FirstOrDefault()?.Reason ?? BlockedReasonCode.EvidenceMissing;

        var explanationLines = new List<string>();
        foreach (var group in reasonGroups)
        {
            explanationLines.Add($"- {GetReasonLabel(group.Reason)}: {group.Count} step(s) affected");
            foreach (var step in group.Steps)
                explanationLines.Add($"  - {step.Title}{(string.IsNullOrEmpty(step.BlockedReasonDetail) ? "" : $": {step.BlockedReasonDetail}")}");
        }

        var content = $"Why is case {caseEntity.CaseNumber} blocked?\n\n"
                      + (blockedSteps.Count == 0
                          ? "This case has no blocked workflow steps."
                          : $"There are {blockedSteps.Count} blocked step(s):\n\n" + string.Join("\n", explanationLines));

        var checklist = reasonGroups.Select(g => $"Resolve {GetReasonLabel(g.Reason)} ({g.Count} step(s))").ToList();
        if (checklist.Count == 0)
            checklist.Add("No blocked steps to resolve");

        return new CopilotDraftResponse
        {
            Id = Guid.NewGuid(),
            DraftType = "explain_why",
            Content = content,
            Checklist = checklist,
            Explainability = new CopilotExplainabilityBlock
            {
                BasedOn = $"{blockedSteps.Count} blocked workflow step(s) in case {caseEntity.CaseNumber}",
                ReasonCategory = primaryReason.ToString(),
                ConfidenceBand = (blockedSteps.Count > 0 ? ConfidenceBand.High : ConfidenceBand.Medium).ToString().ToLowerInvariant(),
                MissingOrUnknown = blockedSteps.Count == 0 ? new List<string> { "No blocked steps found; case may be progressing normally" } : new List<string>(),
                SafeFallback = "Contact case manager for additional context."
            },
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };
    }

    private static (List<string> Steps, string Fallback) BuildRecoverySteps(BlockedReasonCode reason)
    {
        return reason switch
        {
            BlockedReasonCode.EvidenceMissing => (
                new List<string>
                {
                    "Identify the specific missing document or information",
                    "Send an evidence request to the family",
                    "Set a follow-up reminder for 3 business days",
                    "Once received, upload and mark the step as ready"
                },
                "Escalate to case manager to contact the family directly."),

            BlockedReasonCode.ExternalDependency => (
                new List<string>
                {
                    "Confirm the external request was submitted",
                    "Check expected response timeline with the institution",
                    "Set a follow-up reminder for the expected date",
                    "Once received, update the workflow step"
                },
                "Escalate to manager; consider alternative institutions or channels."),

            BlockedReasonCode.PolicyRestriction => (
                new List<string>
                {
                    "Review the policy rule that triggered the restriction",
                    "Check if conditions can be met with available information",
                    "Request approval from an authorized administrator",
                    "Document the approval and unblock the step"
                },
                "Escalate to admin for policy override consideration."),

            BlockedReasonCode.DeadlineRisk => (
                new List<string>
                {
                    "Identify the deadline and remaining time",
                    "Prioritize the blocked step above other tasks",
                    "Assign to the most available team member",
                    "If deadline cannot be met, notify the manager immediately"
                },
                "Escalate to manager for deadline extension or risk acceptance."),

            BlockedReasonCode.PaymentOrBilling => (
                new List<string>
                {
                    "Check the current billing status and outstanding amount",
                    "Contact the billing contact for the tenant",
                    "If payment is in transit, extend the grace period",
                    "Once payment clears, unblock the affected steps"
                },
                "Escalate to admin for billing support."),

            BlockedReasonCode.DataMismatch => (
                new List<string>
                {
                    "Identify the conflicting data fields",
                    "Request clarification from the data source",
                    "Reconcile the data and update the case record",
                    "Re-validate the affected workflow step"
                },
                "Escalate to manager to determine authoritative source."),

            _ => (
                new List<string>
                {
                    "Review the blocked step details",
                    "Contact the appropriate team or resource",
                    "Document the resolution steps taken",
                    "Update the workflow step status"
                },
                "Escalate to support for further investigation.")
        };
    }

    private static string GetReasonLabel(BlockedReasonCode reason)
    {
        return reason switch
        {
            BlockedReasonCode.EvidenceMissing => "Missing information or document",
            BlockedReasonCode.ExternalDependency => "Waiting on an external institution",
            BlockedReasonCode.PolicyRestriction => "Blocked by policy",
            BlockedReasonCode.RolePermission => "Permission issue",
            BlockedReasonCode.DeadlineRisk => "Deadline at risk",
            BlockedReasonCode.PaymentOrBilling => "Billing issue",
            BlockedReasonCode.IdentityOrAuth => "Identity or access problem",
            BlockedReasonCode.DataMismatch => "Information conflict",
            BlockedReasonCode.SystemError => "System problem",
            _ => "Unknown reason"
        };
    }
}
