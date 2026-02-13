using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class SupportDiagnosticSession
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public SupportDiagnosticScope Scope { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
