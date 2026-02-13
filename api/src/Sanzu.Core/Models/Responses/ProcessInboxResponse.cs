namespace Sanzu.Core.Models.Responses;

public sealed class ProcessInboxResponse
{
    public Guid CaseId { get; init; }
    public DateTime RetrievedAt { get; init; }
    public IReadOnlyList<ProcessInboxThreadResponse> Threads { get; init; } = [];
}
