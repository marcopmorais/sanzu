namespace Sanzu.Core.Models.Responses;

public sealed class BillingHistoryResponse
{
    public Guid TenantId { get; init; }
    public IReadOnlyList<BillingRecordResponse> Records { get; init; } = Array.Empty<BillingRecordResponse>();
    public string CurrentPlan { get; init; } = string.Empty;
    public string CurrentBillingCycle { get; init; } = string.Empty;
    public decimal CurrentMonthlyPrice { get; init; }
}
