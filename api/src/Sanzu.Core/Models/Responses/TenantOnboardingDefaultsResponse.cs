namespace Sanzu.Core.Models.Responses;

public sealed class TenantOnboardingDefaultsResponse
{
    public Guid TenantId { get; init; }
    public string DefaultLocale { get; init; } = string.Empty;
    public string DefaultTimeZone { get; init; } = string.Empty;
    public string DefaultCurrency { get; init; } = string.Empty;
    public string? DefaultWorkflowKey { get; init; }
    public string? DefaultTemplateKey { get; init; }
}
