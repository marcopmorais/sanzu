using Sanzu.Core.Entities;
using Sanzu.Core.Enums;
using Sanzu.Core.Interfaces;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Services;

public sealed class WorkflowBlockedStateService : IWorkflowBlockedStateService
{
    private readonly IWorkflowStepRepository _workflowStepRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICaseParticipantRepository _caseParticipantRepository;

    public WorkflowBlockedStateService(
        IWorkflowStepRepository workflowStepRepository,
        IUserRoleRepository userRoleRepository,
        ICaseParticipantRepository caseParticipantRepository)
    {
        _workflowStepRepository = workflowStepRepository;
        _userRoleRepository = userRoleRepository;
        _caseParticipantRepository = caseParticipantRepository;
    }

    public async Task<WorkflowStepBlockedInfo?> GetBlockedInfoAsync(
        WorkflowStepInstance step,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        // Only provide blocked info if step is actually blocked
        if (step.Status != WorkflowStepStatus.Blocked &&
            step.Status != WorkflowStepStatus.AwaitingEvidence)
        {
            return null;
        }

        // Determine reason code and detail
        var (reasonCode, reasonDetail) = await DeriveBlockedReasonAsync(step, cancellationToken);

        // Get allowed recovery actions based on reason and user role
        var allowedActions = await DeriveAllowedActionsAsync(
            step,
            reasonCode,
            actorUserId,
            cancellationToken);

        return new WorkflowStepBlockedInfo
        {
            ReasonCode = reasonCode,
            ReasonLabel = GetReasonLabel(reasonCode),
            ReasonDetail = reasonDetail,
            AllowedActions = allowedActions
        };
    }

    private async Task<(BlockedReasonCode, string)> DeriveBlockedReasonAsync(
        WorkflowStepInstance step,
        CancellationToken cancellationToken)
    {
        // If reason is already persisted, use it
        if (step.BlockedReasonCode.HasValue && !string.IsNullOrWhiteSpace(step.BlockedReasonDetail))
        {
            return (step.BlockedReasonCode.Value, step.BlockedReasonDetail);
        }

        // Otherwise, derive from step state

        // Check if waiting for evidence
        if (step.Status == WorkflowStepStatus.AwaitingEvidence)
        {
            return (BlockedReasonCode.EvidenceMissing,
                "This step requires documents or information that have not been uploaded yet.");
        }

        // Check for dependency blocks
        var dependencies = await _workflowStepRepository.GetDependenciesAsync(step.Id, cancellationToken);
        var incompleteDeps = dependencies.Where(d => d.DependsOnStep?.Status != WorkflowStepStatus.Complete).ToList();

        if (incompleteDeps.Any())
        {
            return (BlockedReasonCode.ExternalDependency,
                $"This step depends on {incompleteDeps.Count} other step(s) that must be completed first.");
        }

        // Check for overdue status (deadline risk)
        if (step.Status == WorkflowStepStatus.Overdue)
        {
            return (BlockedReasonCode.DeadlineRisk,
                "This step is overdue and requires immediate attention.");
        }

        // Default to policy restriction if blocked but reason unclear
        return (BlockedReasonCode.PolicyRestriction,
            "This step is blocked by policy rules and cannot proceed yet.");
    }

    private async Task<IReadOnlyList<AllowedRecoveryActionInfo>> DeriveAllowedActionsAsync(
        WorkflowStepInstance step,
        BlockedReasonCode reasonCode,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actions = new List<AllowedRecoveryActionInfo>();

        // Get user's role in this case
        var participant = await _caseParticipantRepository.GetAcceptedParticipantAsync(
            step.CaseId,
            actorUserId,
            cancellationToken);

        var isEditor = participant?.Role == CaseRole.Editor || participant?.Role == CaseRole.Manager;
        var isManager = participant?.Role == CaseRole.Manager;

        // Derive actions based on reason code
        switch (reasonCode)
        {
            case BlockedReasonCode.EvidenceMissing:
                if (isEditor)
                {
                    actions.Add(new AllowedRecoveryActionInfo
                    {
                        Action = RecoveryAction.UploadEvidence,
                        Label = "Upload required documents",
                        Guidance = "Upload the missing documents or information to unblock this step.",
                        IsAvailable = true
                    });
                }
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.ContactManager,
                    Label = "Contact case manager",
                    Guidance = "Reach out to your case manager for help with what's needed.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.ExternalDependency:
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.CompletePrerequisite,
                    Label = "Complete prerequisite steps",
                    Guidance = "Finish the required steps before returning to this one.",
                    IsAvailable = isEditor
                });
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.WaitForExternal,
                    Label = "Wait for external update",
                    Guidance = "This step depends on external institutions and may take time.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.PolicyRestriction:
                if (isManager)
                {
                    actions.Add(new AllowedRecoveryActionInfo
                    {
                        Action = RecoveryAction.RequestOverride,
                        Label = "Request policy override",
                        Guidance = "Submit an override request with rationale for approval.",
                        IsAvailable = true
                    });
                }
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.ContactManager,
                    Label = "Contact case manager",
                    Guidance = "Discuss policy restrictions with your case manager.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.RolePermission:
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.RequestPermission,
                    Label = "Request permission",
                    Guidance = "Ask an administrator to grant you the required role permission.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.DeadlineRisk:
                if (isEditor)
                {
                    actions.Add(new AllowedRecoveryActionInfo
                    {
                        Action = RecoveryAction.UploadEvidence,
                        Label = "Complete this step now",
                        Guidance = "This step is overdue. Complete it immediately to avoid delays.",
                        IsAvailable = true
                    });
                }
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.ContactManager,
                    Label = "Contact case manager urgently",
                    Guidance = "Reach out to your case manager about the deadline immediately.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.DataMismatch:
                if (isEditor)
                {
                    actions.Add(new AllowedRecoveryActionInfo
                    {
                        Action = RecoveryAction.CorrectData,
                        Label = "Review and correct information",
                        Guidance = "Check the conflicting data and make necessary corrections.",
                        IsAvailable = true
                    });
                }
                break;

            case BlockedReasonCode.SystemError:
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.ContactSupport,
                    Label = "Contact technical support",
                    Guidance = "Report this technical issue to support for assistance.",
                    IsAvailable = true
                });
                break;

            case BlockedReasonCode.PaymentOrBilling:
                actions.Add(new AllowedRecoveryActionInfo
                {
                    Action = RecoveryAction.UpdateBilling,
                    Label = "Update billing information",
                    Guidance = "Go to billing settings to resolve payment issues.",
                    IsAvailable = true
                });
                break;
        }

        return actions;
    }

    private static string GetReasonLabel(BlockedReasonCode reasonCode)
    {
        return reasonCode switch
        {
            BlockedReasonCode.EvidenceMissing => "Missing information or document",
            BlockedReasonCode.ExternalDependency => "Waiting on an external institution",
            BlockedReasonCode.PolicyRestriction => "Blocked by policy",
            BlockedReasonCode.RolePermission => "You do not have permission",
            BlockedReasonCode.DeadlineRisk => "Deadline at risk",
            BlockedReasonCode.PaymentOrBilling => "Billing issue",
            BlockedReasonCode.IdentityOrAuth => "Identity or access problem",
            BlockedReasonCode.DataMismatch => "Information conflict",
            BlockedReasonCode.SystemError => "System problem",
            _ => "Blocked"
        };
    }
}
