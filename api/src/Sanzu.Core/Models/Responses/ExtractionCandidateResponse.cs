namespace Sanzu.Core.Models.Responses;

public sealed class ExtractionCandidateResponse
{
    public Guid CandidateId { get; init; }
    public string FieldKey { get; init; } = string.Empty;
    public string CandidateValue { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public int SourceVersionNumber { get; init; }
    public string Status { get; init; } = string.Empty;
}
