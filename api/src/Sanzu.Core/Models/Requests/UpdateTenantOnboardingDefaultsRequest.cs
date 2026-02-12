namespace Sanzu.Core.Models.Requests;

public sealed class UpdateTenantOnboardingDefaultsRequest
{
    public string DefaultLocale { get; set; } = string.Empty;
    public string DefaultTimeZone { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public string? DefaultWorkflowKey { get; set; }
    public string? DefaultTemplateKey { get; set; }
}
