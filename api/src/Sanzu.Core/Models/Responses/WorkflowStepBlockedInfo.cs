using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

/// <summary>
/// Blocked state information for a workflow step
/// </summary>
public sealed class WorkflowStepBlockedInfo
{
    /// <summary>
    /// Canonical reason code for why the step is blocked
    /// </summary>
    public BlockedReasonCode ReasonCode { get; init; }

    /// <summary>
    /// Plain-language label for the reason (e.g., "Missing information or document")
    /// </summary>
    public string ReasonLabel { get; init; } = string.Empty;

    /// <summary>
    /// Plain-language explanation of why this step is blocked
    /// </summary>
    public string ReasonDetail { get; init; } = string.Empty;

    /// <summary>
    /// List of allowed recovery actions for this blocked state
    /// </summary>
    public IReadOnlyList<AllowedRecoveryActionInfo> AllowedActions { get; init; } = new List<AllowedRecoveryActionInfo>();
}

/// <summary>
/// Information about an allowed recovery action
/// </summary>
public sealed class AllowedRecoveryActionInfo
{
    /// <summary>
    /// Recovery action code
    /// </summary>
    public RecoveryAction Action { get; init; }

    /// <summary>
    /// Plain-language label for the action
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Detailed guidance on how to perform this action
    /// </summary>
    public string Guidance { get; init; } = string.Empty;

    /// <summary>
    /// Whether this action is available to the current user based on their role
    /// </summary>
    public bool IsAvailable { get; init; }
}
