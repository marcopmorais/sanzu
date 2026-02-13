namespace Sanzu.Core.Models.Requests;

public sealed class UpdateTenantCaseDefaultsRequest
{
    public string? DefaultWorkflowKey { get; init; }
    public string? DefaultTemplateKey { get; init; }
}
