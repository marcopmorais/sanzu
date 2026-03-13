namespace Sanzu.Core.Models.Requests;

public sealed class TenantListRequest
{
    public string? Name { get; set; }
    public string? Status { get; set; }
    public string? HealthBand { get; set; }
    public string? PlanTier { get; set; }
    public DateTime? SignupDateFrom { get; set; }
    public DateTime? SignupDateTo { get; set; }
    public string? Sort { get; set; }
    public string? Order { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
