namespace Sanzu.Core.Models.Requests;

public sealed class ApplyExtractionDecisionsRequest
{
    public IReadOnlyList<ExtractionDecisionRequest> Decisions { get; init; } = [];
}
