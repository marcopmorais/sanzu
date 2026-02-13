namespace Sanzu.Core.Models.Responses;

public sealed class CaseHandoffEvidenceItemResponse
{
    public Guid DocumentId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public int VersionNumber { get; init; }
    public string Classification { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}
