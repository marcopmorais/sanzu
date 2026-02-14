namespace Sanzu.Core.Models.Responses;

public sealed class ChangePlanResponse
{
    public Guid TenantId { get; init; }
    public string PlanCode { get; init; } = string.Empty;
    public string BillingCycle { get; init; } = string.Empty;
    public string PreviousPlan { get; init; } = string.Empty;
    public string PreviousBillingCycle { get; init; } = string.Empty;
    public DateTime EffectiveDate { get; init; }
    public decimal ProrationAmount { get; init; }
    public DateTime ChangedAt { get; init; }
}
