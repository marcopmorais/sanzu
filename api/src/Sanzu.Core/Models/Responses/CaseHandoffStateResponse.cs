namespace Sanzu.Core.Models.Responses;

public sealed class CaseHandoffStateResponse
{
    public Guid HandoffId { get; init; }
    public Guid CaseId { get; init; }
    public string PacketTitle { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool FollowUpRequired { get; init; }
    public string? StatusNotes { get; init; }
    public Guid LastUpdatedByUserId { get; init; }
    public DateTime LastStatusChangedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
