namespace Sanzu.Core.Models.Responses;

public sealed class CaseTaskWorkspaceResponse
{
    public Guid CaseId { get; init; }
    public DateTime RetrievedAt { get; init; }
    public IReadOnlyList<CaseTaskItemResponse> Tasks { get; init; } = Array.Empty<CaseTaskItemResponse>();
}
