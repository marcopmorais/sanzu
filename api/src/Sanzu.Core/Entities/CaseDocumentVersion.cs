namespace Sanzu.Core.Entities;

public sealed class CaseDocumentVersion
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public Guid UploadedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public CaseDocument? Document { get; set; }
}
