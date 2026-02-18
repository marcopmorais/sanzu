namespace Sanzu.Core.Models.Responses;

public sealed class FleetPostureResponse
{
    public int TotalTenants { get; init; }
    public int ActiveTenants { get; init; }
    public int OnboardingTenants { get; init; }
    public int PaymentIssueTenants { get; init; }
    public int SuspendedTenants { get; init; }
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<TenantPostureResponse> Tenants { get; init; } = [];
}

public sealed class TenantPostureResponse
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? SubscriptionPlan { get; init; }
    public int ActiveCases { get; init; }
    public int BlockedTasks { get; init; }
    public int OpenKpiAlerts { get; init; }
    public int FailedPaymentAttempts { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? OnboardingCompletedAt { get; init; }
}

public sealed class TenantDrilldownResponse
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? SubscriptionPlan { get; init; }
    public string? SubscriptionBillingCycle { get; init; }
    public int FailedPaymentAttempts { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? OnboardingCompletedAt { get; init; }
    public DateTime? SubscriptionActivatedAt { get; init; }
    public TenantDrilldownMetrics Metrics { get; init; } = new();
    public IReadOnlyList<ReasonCodeCountResponse> BlockedByReason { get; init; } = [];
}

public sealed class TenantDrilldownMetrics
{
    public int TotalCases { get; init; }
    public int ActiveCases { get; init; }
    public int ClosedCases { get; init; }
    public int BlockedTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int DocumentsUploaded { get; init; }
}
