namespace Sanzu.Core.Models.Responses;

public sealed class CaseDocumentUploadResponse
{
    public Guid DocumentId { get; init; }
    public Guid CaseId { get; init; }
    public int VersionNumber { get; init; }
    public string Classification { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
}
