namespace Sanzu.Core.Models.Responses;

public sealed class ExtractDocumentCandidatesResponse
{
    public Guid CaseId { get; init; }
    public Guid DocumentId { get; init; }
    public int SourceVersionNumber { get; init; }
    public DateTime ExtractedAt { get; init; }
    public IReadOnlyList<ExtractionCandidateResponse> Candidates { get; init; } = [];
}
