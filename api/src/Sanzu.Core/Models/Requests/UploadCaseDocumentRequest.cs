namespace Sanzu.Core.Models.Requests;

public sealed class UploadCaseDocumentRequest
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public string ContentBase64 { get; init; } = string.Empty;
}
