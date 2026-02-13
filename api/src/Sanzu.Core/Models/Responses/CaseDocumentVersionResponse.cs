namespace Sanzu.Core.Models.Responses;

public sealed class CaseDocumentVersionResponse
{
    public Guid VersionId { get; init; }
    public Guid DocumentId { get; init; }
    public int VersionNumber { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTime UploadedAt { get; init; }
}
