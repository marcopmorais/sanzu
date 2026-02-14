namespace Sanzu.Core.Models.Requests;

public sealed class StartAccountIntentRequest
{
    public string AgencyName { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool TermsAccepted { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? ReferrerPath { get; set; }
    public string? LandingPath { get; set; }
}
