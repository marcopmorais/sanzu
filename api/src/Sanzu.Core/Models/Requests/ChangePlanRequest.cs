namespace Sanzu.Core.Models.Requests;

public sealed class ChangePlanRequest
{
    public string PlanCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal ConfirmedProrationAmount { get; set; }
}
