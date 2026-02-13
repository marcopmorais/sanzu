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
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseDocument> CaseDocuments => Set<CaseDocument>();
    public DbSet<CaseDocumentVersion> CaseDocumentVersions => Set<CaseDocumentVersion>();
    public DbSet<CaseParticipant> CaseParticipants => Set<CaseParticipant>();
    public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
    public DbSet<WorkflowStepDependency> WorkflowStepDependencies => Set<WorkflowStepDependency>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
    public DbSet<BillingRecord> BillingRecords => Set<BillingRecord>();

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

        modelBuilder.Entity<Case>()
            .HasQueryFilter(caseEntity => CurrentOrganizationId == null || caseEntity.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<CaseDocument>()
            .HasQueryFilter(document => CurrentOrganizationId == null || document.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<CaseDocumentVersion>()
            .HasQueryFilter(version => CurrentOrganizationId == null || version.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<CaseParticipant>()
            .HasQueryFilter(participant => CurrentOrganizationId == null || participant.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<WorkflowStepInstance>()
            .HasQueryFilter(step => CurrentOrganizationId == null || step.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<WorkflowStepDependency>()
            .HasQueryFilter(dependency => CurrentOrganizationId == null || dependency.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<TenantInvitation>()
            .HasQueryFilter(invite => CurrentOrganizationId == null || invite.TenantId == CurrentOrganizationId);

        modelBuilder.Entity<BillingRecord>()
            .HasQueryFilter(record => CurrentOrganizationId == null || record.TenantId == CurrentOrganizationId);
    }
}
