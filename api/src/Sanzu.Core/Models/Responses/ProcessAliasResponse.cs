namespace Sanzu.Core.Models.Responses;

public sealed class ProcessAliasResponse
{
    public Guid AliasId { get; init; }
    public Guid CaseId { get; init; }
    public string AliasEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public Guid? RotatedFromAliasId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
