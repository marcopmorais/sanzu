using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CaseMilestoneResponse
{
    public string EventType { get; init; } = string.Empty;
    public CaseStatus? Status { get; init; }
    public string Description { get; init; } = string.Empty;
    public Guid ActorUserId { get; init; }
    public DateTime OccurredAt { get; init; }
}
