namespace Sanzu.Core.Models.Requests;

public sealed class EvaluateKpiAlertsRequest
{
    public int PeriodDays { get; init; } = 30;
    public int TenantLimit { get; init; } = 10;
    public int CaseLimit { get; init; } = 10;
}
