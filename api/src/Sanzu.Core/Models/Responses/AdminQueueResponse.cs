namespace Sanzu.Core.Models.Responses;

public sealed class AdminQueueListResponse
{
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<AdminQueueSummary> Queues { get; init; } = [];
}

public sealed class AdminQueueSummary
{
    public string QueueId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public int ItemCount { get; init; }
}

public sealed class AdminQueueItemsResponse
{
    public string QueueId { get; init; } = string.Empty;
    public string QueueName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<AdminQueueItem> Items { get; init; } = [];
}

public sealed class AdminQueueItem
{
    public string ItemId { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string ReasonCategory { get; init; } = string.Empty;
    public string ReasonLabel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
}

public sealed class AdminEventStreamResponse
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<AdminEventStreamEntry> Events { get; init; } = [];
}

public sealed class AdminEventStreamEntry
{
    public Guid EventId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? ReasonCategory { get; init; }
    public string SafeSummary { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
