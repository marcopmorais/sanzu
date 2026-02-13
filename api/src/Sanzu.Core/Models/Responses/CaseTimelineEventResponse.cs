namespace Sanzu.Core.Models.Responses;

public sealed class CaseTimelineEventResponse
{
    public string EventType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? ActorUserId { get; init; }
    public DateTime OccurredAt { get; init; }
}
