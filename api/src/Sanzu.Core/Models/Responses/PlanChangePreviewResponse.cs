namespace Sanzu.Core.Models.Responses;

public sealed class PlanChangePreviewResponse
{
    public string CurrentPlan { get; init; } = string.Empty;
    public string NewPlan { get; init; } = string.Empty;
    public string CurrentBillingCycle { get; init; } = string.Empty;
    public string NewBillingCycle { get; init; } = string.Empty;
    public decimal CurrentMonthlyPrice { get; init; }
    public decimal NewMonthlyPrice { get; init; }
    public decimal ProrationAmount { get; init; }
    public DateTime EffectiveDate { get; init; }
    public string Description { get; init; } = string.Empty;
}
