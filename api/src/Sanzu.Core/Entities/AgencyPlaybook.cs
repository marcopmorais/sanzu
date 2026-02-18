using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class AgencyPlaybook
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public PlaybookStatus Status { get; set; } = PlaybookStatus.Draft;
    public string? ChangeNotes { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ActivatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ActivatedAt { get; set; }
}
