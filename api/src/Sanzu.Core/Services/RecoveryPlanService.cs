using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Exceptions;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Requests;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class RecoveryPlanService : IRecoveryPlanService
{
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseRepository _caseRepository;
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly IUnitOfWork _unitOfWork;

    private const string BoundaryMessage =
        "Copilot cannot perform autonomous changes to case lifecycle, tenant policies, or user roles. "
        + "All actions require explicit confirmation from an authorized user.";

    public RecoveryPlanService(
        IUserRoleRepository userRoleRepository,
        ICaseRepository caseRepository,
        IWorkflowStepRepository workflowStepRepository,
        IAuditRepository auditRepository,
        IUnitOfWork unitOfWork)
    {
        _userRoleRepository = userRoleRepository;
        _caseRepository = caseRepository;
        _workflowStepRepository = workflowStepRepository;
        _auditRepository = auditRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<RecoveryPlanResponse> GenerateRecoveryPlanAsync(
        Guid actorUserId, Guid tenantId, RequestRecoveryPlanRequest request, CancellationToken cancellationToken)
    {
        var hasRole = await _userRoleRepository.HasRoleAsync(actorUserId, tenantId, PlatformRole.AgencyAdmin, cancellationToken);
        if (!hasRole)
            throw new TenantAccessDeniedException();

        var caseEntity = await _caseRepository.GetByIdAsync(request.CaseId, cancellationToken)
                         ?? throw new CaseStateException("Case not found.");

        var plan = await BuildPlan(caseEntity, request.WorkflowStepId, cancellationToken);

        await RecordAuditEvent(actorUserId, caseEntity.Id, plan.ReasonCategory, cancellationToken);

        return plan;
    }

    public async Task<RecoveryPlanResponse> GeneratePlatformRecoveryPlanAsync(
        Guid actorUserId, Guid tenantId, Guid caseId, CancellationToken cancellationToken)
    {
        // SanzuAdmin authorization is enforced by the controller policy attribute
        var caseEntity = await _caseRepository.GetByIdForPlatformAsync(caseId, cancellationToken)
                         ?? throw new CaseStateException("Case not found.");

        var plan = await BuildPlan(caseEntity, null, cancellationToken);

        await RecordAuditEvent(actorUserId, caseEntity.Id, plan.ReasonCategory, cancellationToken);

        return plan;
    }

    private async Task<RecoveryPlanResponse> BuildPlan(
        Case caseEntity, Guid? workflowStepId, CancellationToken cancellationToken)
    {
        var steps = await _workflowStepRepository.GetByCaseIdAsync(caseEntity.Id, cancellationToken);
        var blockedSteps = steps.Where(s => s.Status == WorkflowStepStatus.Blocked).ToList();

        WorkflowStepInstance? targetStep = null;
        if (workflowStepId.HasValue)
            targetStep = steps.FirstOrDefault(s => s.Id == workflowStepId.Value);
        targetStep ??= blockedSteps.FirstOrDefault();

        var reasonCode = targetStep?.BlockedReasonCode ?? BlockedReasonCode.EvidenceMissing;
        var reasonLabel = GetReasonLabel(reasonCode);

        var (recoverySteps, escalation) = BuildRecoverySteps(reasonCode);
        var evidenceChecklist = BuildEvidenceChecklist(blockedSteps);
        var confidence = targetStep != null ? ConfidenceBand.High : ConfidenceBand.Low;

        var explanation = targetStep != null
            ? $"Case {caseEntity.CaseNumber} has step \"{targetStep.Title}\" blocked due to: {reasonLabel}."
              + (string.IsNullOrEmpty(targetStep.BlockedReasonDetail) ? "" : $" Detail: {targetStep.BlockedReasonDetail}")
            : $"Case {caseEntity.CaseNumber} has {blockedSteps.Count} blocked step(s). No specific step was targeted.";

        return new RecoveryPlanResponse
        {
            Id = Guid.NewGuid(),
            CaseId = caseEntity.Id,
            ReasonCategory = reasonCode.ToString(),
            ReasonLabel = reasonLabel,
            Explanation = explanation,
            Steps = recoverySteps,
            EvidenceChecklist = evidenceChecklist,
            Escalation = escalation,
            Explainability = new CopilotExplainabilityBlock
            {
                BasedOn = $"Blocked step \"{targetStep?.Title ?? "none"}\" with reason {reasonCode} in case {caseEntity.CaseNumber}",
                ReasonCategory = reasonCode.ToString(),
                ConfidenceBand = confidence.ToString().ToLowerInvariant(),
                MissingOrUnknown = targetStep == null
                    ? new List<string> { "No specific blocked step identified" }
                    : new List<string>(),
                SafeFallback = escalation.Instruction
            },
            BoundaryMessage = BoundaryMessage,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task RecordAuditEvent(Guid actorUserId, Guid caseId, string reasonCategory, CancellationToken cancellationToken)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _auditRepository.CreateAsync(new AuditEvent
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                ActorUserId = actorUserId,
                EventType = "RecoveryPlanGenerated",
                Metadata = $"{{\"reasonCategory\":\"{reasonCategory}\"}}",
                CreatedAt = DateTime.UtcNow
            }, ct);
        }, cancellationToken);
    }

    private static List<string> BuildEvidenceChecklist(List<WorkflowStepInstance> blockedSteps)
    {
        var checklist = new List<string>();
        foreach (var step in blockedSteps)
        {
            checklist.Add($"Evidence for: {step.Title}");
            if (!string.IsNullOrEmpty(step.BlockedReasonDetail))
                checklist.Add($"  - {step.BlockedReasonDetail}");
        }
        if (checklist.Count == 0)
            checklist.Add("No specific evidence requirements identified");
        return checklist;
    }

    private static (List<RecoveryStep> Steps, RecoveryEscalation Escalation) BuildRecoverySteps(BlockedReasonCode reason)
    {
        return reason switch
        {
            BlockedReasonCode.EvidenceMissing => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Identify the specific missing document or information", Owner = "Case Manager" },
                    new() { Order = 2, Description = "Send an evidence request to the family", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Set a follow-up reminder for 3 business days", Owner = "Case Manager" },
                    new() { Order = 4, Description = "Once received, upload and mark the step as ready", Owner = "Case Manager" }
                },
                new RecoveryEscalation { TargetRole = "manager", Instruction = "Escalate to case manager to contact the family directly." }),

            BlockedReasonCode.ExternalDependency => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Confirm the external request was submitted", Owner = "Case Manager" },
                    new() { Order = 2, Description = "Check expected response timeline with the institution", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Set a follow-up reminder for the expected date", Owner = "Case Manager" },
                    new() { Order = 4, Description = "Once received, update the workflow step", Owner = "Case Manager" }
                },
                new RecoveryEscalation { TargetRole = "manager", Instruction = "Escalate to manager; consider alternative institutions or channels." }),

            BlockedReasonCode.PolicyRestriction => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Review the policy rule that triggered the restriction", Owner = "Admin" },
                    new() { Order = 2, Description = "Check if conditions can be met with available information", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Request approval from an authorized administrator", Owner = "Case Manager" },
                    new() { Order = 4, Description = "Document the approval and unblock the step", Owner = "Admin" }
                },
                new RecoveryEscalation { TargetRole = "admin", Instruction = "Escalate to admin for policy override consideration." }),

            BlockedReasonCode.DeadlineRisk => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Identify the deadline and remaining time", Owner = "Case Manager" },
                    new() { Order = 2, Description = "Prioritize the blocked step above other tasks", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Assign to the most available team member", Owner = "Case Manager" },
                    new() { Order = 4, Description = "If deadline cannot be met, notify the manager immediately", Owner = "Case Manager" }
                },
                new RecoveryEscalation { TargetRole = "manager", Instruction = "Escalate to manager for deadline extension or risk acceptance." }),

            BlockedReasonCode.PaymentOrBilling => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Check the current billing status and outstanding amount", Owner = "Admin" },
                    new() { Order = 2, Description = "Contact the billing contact for the tenant", Owner = "Admin" },
                    new() { Order = 3, Description = "If payment is in transit, extend the grace period", Owner = "Admin" },
                    new() { Order = 4, Description = "Once payment clears, unblock the affected steps", Owner = "Admin" }
                },
                new RecoveryEscalation { TargetRole = "admin", Instruction = "Escalate to admin for billing support." }),

            BlockedReasonCode.DataMismatch => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Identify the conflicting data fields", Owner = "Case Manager" },
                    new() { Order = 2, Description = "Request clarification from the data source", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Reconcile the data and update the case record", Owner = "Case Manager" },
                    new() { Order = 4, Description = "Re-validate the affected workflow step", Owner = "Case Manager" }
                },
                new RecoveryEscalation { TargetRole = "manager", Instruction = "Escalate to manager to determine authoritative source." }),

            _ => (
                new List<RecoveryStep>
                {
                    new() { Order = 1, Description = "Review the blocked step details", Owner = "Case Manager" },
                    new() { Order = 2, Description = "Contact the appropriate team or resource", Owner = "Case Manager" },
                    new() { Order = 3, Description = "Document the resolution steps taken", Owner = "Case Manager" },
                    new() { Order = 4, Description = "Update the workflow step status", Owner = "Case Manager" }
                },
                new RecoveryEscalation { TargetRole = "support", Instruction = "Escalate to support for further investigation." })
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
