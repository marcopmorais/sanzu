namespace Sanzu.Core.Models.Responses;

public sealed class InvoiceDownloadResponse
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string InvoiceSnapshot { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public DateTime CreatedAt { get; init; }
}
