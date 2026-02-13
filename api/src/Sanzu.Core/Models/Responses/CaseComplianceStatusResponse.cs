using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CaseComplianceStatusResponse
{
    public Guid CaseId { get; init; }
    public string CaseNumber { get; init; } = string.Empty;
    public CaseStatus CaseStatus { get; init; }
    public string PolicyState { get; init; } = string.Empty;
    public IReadOnlyList<string> Exceptions { get; init; } = [];
    public DateTime LastEvaluatedAt { get; init; }
}
