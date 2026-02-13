namespace Sanzu.Core.Models.Responses;

public sealed class CaseDocumentVersionHistoryResponse
{
    public Guid DocumentId { get; init; }
    public Guid CaseId { get; init; }
    public string Classification { get; init; } = string.Empty;
    public int LatestVersionNumber { get; init; }
    public IReadOnlyList<CaseDocumentVersionResponse> Versions { get; init; } = [];
}
