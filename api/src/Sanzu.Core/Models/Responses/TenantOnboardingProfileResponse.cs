using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class TenantOnboardingProfileResponse
{
    public Guid TenantId { get; init; }
    public string AgencyName { get; init; } = string.Empty;
    public string Location { get; init; } = string.Empty;
    public TenantStatus TenantStatus { get; init; }
}
