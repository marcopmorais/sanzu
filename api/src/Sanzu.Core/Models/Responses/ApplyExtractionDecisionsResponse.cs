namespace Sanzu.Core.Models.Responses;

public sealed class ApplyExtractionDecisionsResponse
{
    public Guid CaseId { get; init; }
    public Guid DocumentId { get; init; }
    public DateTime ReviewedAt { get; init; }
    public int TotalDecisions { get; init; }
    public int AppliedCount { get; init; }
    public int RejectedCount { get; init; }
    public IReadOnlyList<ExtractionCandidateResponse> Candidates { get; init; } = [];
}
