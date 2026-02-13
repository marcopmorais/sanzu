namespace Sanzu.Core.Models.Responses;

public sealed class GenerateCasePlanResponse
{
    public Guid CaseId { get; init; }
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<CasePlanStepResponse> Steps { get; init; } = Array.Empty<CasePlanStepResponse>();
}
