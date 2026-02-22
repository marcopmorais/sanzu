namespace Sanzu.Core.Entities;

public sealed class AlertDeliveryConfig
{
    public Guid Id { get; set; }
    public string Channel { get; set; } = string.Empty;  // "email" | "slack"
    public string Target { get; set; } = string.Empty;    // email address or webhook URL
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
