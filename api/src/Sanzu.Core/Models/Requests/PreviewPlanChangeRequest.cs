namespace Sanzu.Core.Models.Requests;

public sealed class PreviewPlanChangeRequest
{
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
}
