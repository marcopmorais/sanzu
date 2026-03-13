namespace Sanzu.Core.Models.Responses;

public sealed class TenantListItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? PlanTier { get; init; }
    public int? HealthScore { get; init; }
    public string? HealthBand { get; init; }
    public DateTime SignupDate { get; init; }
    public string? Region { get; init; }
}
