namespace Sanzu.Core.Entities;

public sealed class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid OrgId { get; set; }
    public string? AzureAdObjectId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserRole> GrantedRoles { get; set; } = new List<UserRole>();
    public ICollection<AuditEvent> ActorAuditEvents { get; set; } = new List<AuditEvent>();
    public ICollection<TenantInvitation> IssuedTenantInvitations { get; set; } = new List<TenantInvitation>();
}
