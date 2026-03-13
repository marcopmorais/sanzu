namespace Sanzu.Core.Entities;

public sealed class TenantCommunication
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SenderUserId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? TemplateId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization? Tenant { get; set; }
    public User? Sender { get; set; }
}
