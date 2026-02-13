namespace Sanzu.Core.Models.Responses;

public sealed class TenantCaseDefaultsResponse
{
    public Guid TenantId { get; init; }
    public string? DefaultWorkflowKey { get; init; }
    public string? DefaultTemplateKey { get; init; }
    public long Version { get; init; }
    public DateTime UpdatedAt { get; init; }
}
