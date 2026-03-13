namespace Sanzu.Core.Models.Responses;

public sealed record AdminDashboardSummary(
    DateTime ComputedAt,
    TenantCounts Tenants,
    RevenuePulse Revenue,
    HealthDistribution Health,
    AlertCounts Alerts,
    OnboardingStatus Onboarding
);

public sealed record TenantCounts(int Total, int Active, int Trial, int Churning, int Suspended);

public sealed record RevenuePulse(decimal Mrr, decimal Arr, decimal ChurnRate, decimal GrowthRate);

public sealed record HealthDistribution(int Green, int Yellow, int Red, IReadOnlyList<AtRiskTenant> TopAtRisk);

public sealed record AtRiskTenant(Guid TenantId, string Name, int Score, string? PrimaryIssue);

public sealed record AlertCounts(int Critical, int Warning, int Info, int Unacknowledged);

public sealed record OnboardingStatus(decimal CompletionRate, int Stalled);
