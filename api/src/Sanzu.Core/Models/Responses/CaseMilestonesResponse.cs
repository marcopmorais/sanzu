namespace Sanzu.Core.Models.Responses;

public sealed class CaseMilestonesResponse
{
    public Guid CaseId { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public IReadOnlyList<CaseMilestoneResponse> Milestones { get; init; } = Array.Empty<CaseMilestoneResponse>();
}
