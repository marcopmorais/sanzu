using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Requests;

public sealed class CreateTenantInvitationRequest
{
    public string Email { get; set; } = string.Empty;
    public PlatformRole RoleType { get; set; } = PlatformRole.AgencyAdmin;
    public int ExpirationDays { get; set; } = 7;
}
