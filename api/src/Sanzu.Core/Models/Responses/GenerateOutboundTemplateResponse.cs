namespace Sanzu.Core.Models.Responses;

public sealed class GenerateOutboundTemplateResponse
{
    public Guid CaseId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/plain";
    public string ContentBase64 { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyDictionary<string, string> MappedFields { get; init; } = new Dictionary<string, string>();
}
