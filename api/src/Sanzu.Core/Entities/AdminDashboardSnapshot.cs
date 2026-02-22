namespace Sanzu.Core.Entities;

public sealed class AdminDashboardSnapshot
{
    public Guid Id { get; set; }
    public string SnapshotType { get; set; } = string.Empty;
    public string JsonPayload { get; set; } = string.Empty;
    public DateTime ComputedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
