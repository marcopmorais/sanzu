using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class AdminAlert
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public AlertStatus Status { get; set; }
    public string RoutedToRole { get; set; } = string.Empty;
    public Guid? OwnedByUserId { get; set; }
    public DateTime FiredAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
