using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class SupportDiagnosticSessionResponse
{
    public Guid SessionId { get; init; }
    public Guid TenantId { get; init; }
    public Guid RequestedByUserId { get; init; }
    public SupportDiagnosticScope Scope { get; init; }
    public int DurationMinutes { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}
