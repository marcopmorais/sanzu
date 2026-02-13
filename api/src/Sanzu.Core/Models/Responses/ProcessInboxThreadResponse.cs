namespace Sanzu.Core.Models.Responses;

public sealed class ProcessInboxThreadResponse
{
    public string ThreadId { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public DateTime LastMessageAt { get; init; }
    public int MessageCount { get; init; }
    public IReadOnlyList<string> Participants { get; init; } = [];
    public string LatestDirection { get; init; } = string.Empty;
    public string CaseContextUrl { get; init; } = string.Empty;
    public IReadOnlyList<ProcessInboxMessageResponse> Messages { get; init; } = [];
}
