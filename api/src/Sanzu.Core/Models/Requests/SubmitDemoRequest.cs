namespace Sanzu.Core.Models.Requests;

public sealed class SubmitDemoRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? ReferrerPath { get; set; }
    public string? LandingPath { get; set; }
}
