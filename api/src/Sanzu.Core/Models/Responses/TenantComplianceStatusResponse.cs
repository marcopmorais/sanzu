namespace Sanzu.Core.Models.Responses;

public sealed class TenantComplianceStatusResponse
{
    public Guid TenantId { get; init; }
    public DateTime RetrievedAt { get; init; }
    public int ExceptionCaseCount { get; init; }
    public IReadOnlyList<CaseComplianceStatusResponse> Cases { get; init; } = [];
}
