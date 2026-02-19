namespace Sanzu.Core.Models.Responses;

public sealed class PlatformOperationsSummaryResponse
{
    public int TotalActiveTenants { get; init; }
    public int TotalActiveCases { get; init; }
    public int WorkflowStepsCompleted { get; init; }
    public int WorkflowStepsActive { get; init; }
    public int WorkflowStepsBlocked { get; init; }
    public int TotalDocuments { get; init; }
}
