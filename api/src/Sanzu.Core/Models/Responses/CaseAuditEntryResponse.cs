namespace Sanzu.Core.Models.Responses;

public sealed class CaseAuditEntryResponse
{
    public Guid AuditEventId { get; init; }
    public Guid ActorUserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; }
    public string ContextJson { get; init; } = "{}";
}
