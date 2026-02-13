using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class SupportDiagnosticSummaryResponse
{
    public Guid SessionId { get; init; }
    public Guid TenantId { get; init; }
    public SupportDiagnosticScope Scope { get; init; }
    public DateTime RetrievedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public TenantStatus TenantStatus { get; init; }
    public int ActiveCaseCount { get; init; }
    public int TotalCaseCount { get; init; }
    public int DiagnosticActionsLast24Hours { get; init; }
}
