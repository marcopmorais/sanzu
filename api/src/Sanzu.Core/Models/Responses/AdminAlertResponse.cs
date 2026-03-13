namespace Sanzu.Core.Models.Responses;

public sealed class AdminAlertResponse
{
    public Guid Id { get; init; }
    public Guid? TenantId { get; init; }
    public string AlertType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string RoutedToRole { get; init; } = string.Empty;
    public Guid? OwnedByUserId { get; init; }
    public DateTime FiredAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public string? TenantName { get; init; }
}
