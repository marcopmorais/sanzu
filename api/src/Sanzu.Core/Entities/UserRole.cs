using Sanzu.Core.Enums;

namespace Sanzu.Core.Entities;

public sealed class UserRole
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public PlatformRole RoleType { get; set; }
    public Guid? TenantId { get; set; }
    public DateTime GrantedAt { get; set; }
    public Guid GrantedBy { get; set; }

    public User? User { get; set; }
    public Organization? Tenant { get; set; }
    public User? GrantedByUser { get; set; }
}
