using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class PlaybookResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Version { get; init; }
    public PlaybookStatus Status { get; init; }
    public string? ChangeNotes { get; init; }
    public Guid CreatedByUserId { get; init; }
    public Guid? ActivatedByUserId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
}
