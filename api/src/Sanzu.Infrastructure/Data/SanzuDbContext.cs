using Microsoft.EntityFrameworkCore;
using Sanzu.Core.Entities;

namespace Sanzu.Infrastructure.Data;

public sealed class SanzuDbContext : DbContext
{
    public SanzuDbContext(DbContextOptions<SanzuDbContext> options)
        : base(options)
    {
    }

    public Guid? CurrentOrganizationId { get; set; }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SanzuDbContext).Assembly);

        modelBuilder.Entity<Organization>()
            .HasQueryFilter(org => CurrentOrganizationId == null || org.Id == CurrentOrganizationId);

        modelBuilder.Entity<User>()
            .HasQueryFilter(user => CurrentOrganizationId == null || user.OrgId == CurrentOrganizationId);

        modelBuilder.Entity<UserRole>()
            .HasQueryFilter(role => CurrentOrganizationId == null || role.TenantId == null || role.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<TenantInvitation>()
            .HasQueryFilter(invite => CurrentOrganizationId == null || invite.TenantId == CurrentOrganizationId);
    }
}
