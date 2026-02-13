namespace Sanzu.Core.Models.Responses;

public sealed class CaseDocumentDownloadResponse
{
    public Guid DocumentId { get; init; }
    public Guid CaseId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public string ContentBase64 { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}
