namespace Sanzu.Core.Models.Responses;

public sealed class CaseTimelineResponse
{
    public Guid CaseId { get; init; }
    public IReadOnlyList<CaseTaskOwnerResponse> CurrentOwners { get; init; } = Array.Empty<CaseTaskOwnerResponse>();
    public IReadOnlyList<CaseTimelineEventResponse> Events { get; init; } = Array.Empty<CaseTimelineEventResponse>();
}
