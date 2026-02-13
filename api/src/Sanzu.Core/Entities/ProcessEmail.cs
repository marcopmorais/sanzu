using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class ProcessEmail
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public Guid ProcessAliasId { get; set; }
    public string ThreadId { get; set; } = string.Empty;
    public ProcessEmailDirection Direction { get; set; } = ProcessEmailDirection.Outbound;
    public string Subject { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string RecipientEmails { get; set; } = string.Empty;
    public string? BodyPreview { get; set; }
    public string? ExternalMessageId { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Case? Case { get; set; }
    public ProcessAlias? ProcessAlias { get; set; }
}
