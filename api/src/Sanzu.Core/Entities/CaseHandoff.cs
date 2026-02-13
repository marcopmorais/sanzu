using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class CaseHandoff
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string PacketTitle { get; set; } = string.Empty;
    public CaseHandoffStatus Status { get; set; } = CaseHandoffStatus.PendingAdvisor;
    public bool FollowUpRequired { get; set; } = true;
    public string? StatusNotes { get; set; }
    public Guid LastUpdatedByUserId { get; set; }
    public DateTime LastStatusChangedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Case? Case { get; set; }
}
