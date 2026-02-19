using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantHealthScoreResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int OverallScore { get; init; }
    public int BillingScore { get; init; }
    public int CaseCompletionScore { get; init; }
    public int OnboardingScore { get; init; }
    public HealthBand HealthBand { get; init; }
    public string? PrimaryIssue { get; init; }
    public DateTime ComputedAt { get; init; }
}
