using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class ProcessAlias
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CaseId { get; set; }
    public string AliasEmail { get; set; } = string.Empty;
    public ProcessAliasStatus Status { get; set; } = ProcessAliasStatus.Active;
    public Guid? RotatedFromAliasId { get; set; }
    public Guid LastUpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Case? Case { get; set; }
    public ProcessAlias? RotatedFromAlias { get; set; }
}
