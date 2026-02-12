namespace Sanzu.Core.Entities;

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid? CaseId { get; set; }
    public Guid ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Metadata { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public User? ActorUser { get; set; }
}
