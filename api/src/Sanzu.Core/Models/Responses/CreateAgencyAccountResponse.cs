using Sanzu.Core.Enums;

namespace Sanzu.Core.Models.Responses;

public sealed class CreateAgencyAccountResponse
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public TenantStatus TenantStatus { get; init; }
}
