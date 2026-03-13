namespace Sanzu.Core.Models.Responses;

public sealed class AuditEventResponse
{
    public Guid Id { get; init; }
    public Guid ActorUserId { get; init; }
    public string ActorName { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public Guid? CaseId { get; init; }
    public string Metadata { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
}

public sealed class AuditSearchResponse
{
    public IReadOnlyList<AuditEventResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int TotalCount { get; init; }
}
