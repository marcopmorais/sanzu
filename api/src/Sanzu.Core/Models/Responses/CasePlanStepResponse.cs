using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CasePlanStepResponse
{
    public Guid StepId { get; init; }
    public string StepKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Sequence { get; init; }
    public WorkflowStepStatus Status { get; init; }
    public IReadOnlyList<Guid> DependsOnStepIds { get; init; } = Array.Empty<Guid>();
}
