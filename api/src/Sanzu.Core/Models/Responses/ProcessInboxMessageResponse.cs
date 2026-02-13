namespace Sanzu.Core.Models.Responses;

public sealed class ProcessInboxMessageResponse
{
    public Guid MessageId { get; init; }
    public string ThreadId { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string SenderEmail { get; init; } = string.Empty;
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
    public string? BodyPreview { get; init; }
    public string? ExternalMessageId { get; init; }
    public DateTime SentAt { get; init; }
}
