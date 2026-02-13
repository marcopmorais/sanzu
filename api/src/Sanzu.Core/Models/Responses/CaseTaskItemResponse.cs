using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CaseTaskItemResponse
{
    public Guid StepId { get; init; }
    public string StepKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public int PriorityRank { get; init; }
    public WorkflowStepStatus Status { get; init; }
    public Guid? AssignedUserId { get; init; }
    public DateTime? DueDate { get; init; }
    public string? DeadlineSource { get; init; }
    public string UrgencyIndicator { get; init; } = "none";
    public IReadOnlyList<Guid> DependsOnStepIds { get; init; } = Array.Empty<Guid>();
}
