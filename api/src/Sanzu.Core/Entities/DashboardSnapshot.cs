namespace Sanzu.Core.Entities;

public sealed class DashboardSnapshot
{
    public Guid Id { get; set; }
    public DateTime ComputedAt { get; set; }
    public bool IsStale { get; set; }
    public int TotalTenants { get; set; }
    public int ActiveTenants { get; set; }
    public int GreenTenants { get; set; }
    public int YellowTenants { get; set; }
    public int RedTenants { get; set; }
    public decimal TotalRevenueMtd { get; set; }
    public int OpenAlerts { get; set; }
    public decimal AvgHealthScore { get; set; }
    public string Metadata { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
