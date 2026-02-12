namespace Sanzu.Core.Models.Requests;

public sealed class UpdateTenantOnboardingProfileRequest
{
    public string AgencyName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
