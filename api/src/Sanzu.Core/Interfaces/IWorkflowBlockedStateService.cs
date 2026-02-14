using Sanzu.Core.Entities;
using Sanzu.Core.Models.Responses;

namespace Sanzu.Core.Interfaces;

/// <summary>
/// Service for deriving blocked state information and recovery actions for workflow steps
/// </summary>
public interface IWorkflowBlockedStateService
{
    /// <summary>
    /// Determines blocked state information for a workflow step if it is blocked
    /// </summary>
    /// <param name="step">The workflow step instance</param>
    /// <param name="actorUserId">The current user viewing the step</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Blocked info if step is blocked, otherwise null</returns>
    Task<WorkflowStepBlockedInfo?> GetBlockedInfoAsync(
        WorkflowStepInstance step,
        Guid actorUserId,
        CancellationToken cancellationToken);
}
