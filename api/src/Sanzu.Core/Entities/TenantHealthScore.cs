using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class TenantHealthScore
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int OverallScore { get; set; }
    public int BillingScore { get; set; }
    public int CaseCompletionScore { get; set; }
    public int OnboardingScore { get; set; }
    public HealthBand HealthBand { get; set; }
    public string? PrimaryIssue { get; set; }
    public DateTime ComputedAt { get; set; }
}
