namespace Sanzu.Core.Models.Responses;

public sealed class CaseAuditTrailResponse
{
    public Guid CaseId { get; init; }
    public IReadOnlyList<CaseAuditEntryResponse> Entries { get; init; } = [];
}
