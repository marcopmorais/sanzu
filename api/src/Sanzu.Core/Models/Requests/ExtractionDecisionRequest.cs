namespace Sanzu.Core.Models.Requests;

public sealed class ExtractionDecisionRequest
{
    public Guid CandidateId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string? EditedValue { get; init; }
}
