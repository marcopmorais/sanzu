namespace Sanzu.Core.Entities;

public sealed class PublicLead
{
    public Guid Id { get; set; }
    public string IntentType { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int TeamSize { get; set; }
    public bool TermsAccepted { get; set; }
    public bool Qualified { get; set; }
    public string RouteTarget { get; set; } = string.Empty;
    public string RouteStatus { get; set; } = string.Empty;
    public string? RouteFailureReason { get; set; }
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? ReferrerPath { get; set; }
    public string? LandingPath { get; set; }
    public string? UserAgent { get; set; }
    public string? ClientIp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
