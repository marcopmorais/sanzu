namespace Sanzu.Core.Models.Responses;

public sealed class CaseDocumentClassificationResponse
{
    public Guid DocumentId { get; init; }
    public Guid CaseId { get; init; }
    public string Classification { get; init; } = string.Empty;
    public DateTime UpdatedAt { get; init; }
}
