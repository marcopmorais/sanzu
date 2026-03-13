namespace Sanzu.Core.Models.Responses;

public sealed class TenantActivityResponse
{
    public IReadOnlyList<TenantActivityItem> Events { get; init; } = [];
}

public sealed class TenantActivityItem
{
    public string EventType { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public DateTime Timestamp { get; init; }
    public Guid? CaseId { get; init; }
    public string Metadata { get; init; } = string.Empty;
}
